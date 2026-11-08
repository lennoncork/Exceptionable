using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal sealed class ExceptionTypeResolver
    {
        private readonly Compilation compilation;
        private readonly INamedTypeSymbol? exceptionBaseType;
        private readonly ConcurrentDictionary<string, ExceptionTypeResolution> cache =
            new(StringComparer.Ordinal);
        private ShortNameIndex? shortNames;

        internal ExceptionTypeResolver(Compilation compilation)
        {
            this.compilation = compilation;
            this.exceptionBaseType = compilation.GetTypeByMetadataName("System.Exception");
        }

        internal INamedTypeSymbol? ResolveExceptionType(string value) =>
            this.Resolve(value).Type;

        internal ExceptionTypeResolution Resolve(string value)
        {
            string trimmed = value.Trim();
            if (trimmed.StartsWith("global::", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring("global::".Length);
            }

            return this.cache.GetOrAdd(trimmed, this.resolveUncached);
        }

        private ExceptionTypeResolution resolveUncached(string trimmed)
        {
            if (string.Equals(trimmed, "Exception", StringComparison.Ordinal))
            {
                return new ExceptionTypeResolution(this.exceptionBaseType, isAmbiguous: false);
            }

            return trimmed.IndexOf('.') >= 0 || trimmed.IndexOf('+') >= 0
                ? this.resolveMetadataName(trimmed)
                : this.resolveShortName(trimmed);
        }

        private ExceptionTypeResolution resolveMetadataName(string name)
        {
            INamedTypeSymbol? match = null;
            bool ambiguous = false;
            bool sawNonException = false;
            this.consider(this.compilation.GetTypeByMetadataName(name), ref match, ref ambiguous, ref sawNonException);
            if (ambiguous)
            {
                return ExceptionTypeResolution.Ambiguous;
            }

            // A metadata name that already uses '+' is exact. A dotted name can also
            // be the C# spelling of a nested type, so consider those forms too.
            if (name.IndexOf('+') < 0 && name.IndexOf('.') >= 0)
            {
                string[] parts = name.Split('.');
                for (int namespaceLength = parts.Length - 1; namespaceLength >= 1; namespaceLength--)
                {
                    string candidate = string.Join(".", parts, 0, namespaceLength) + "+" +
                        string.Join("+", parts, namespaceLength, parts.Length - namespaceLength);
                    this.consider(
                        this.compilation.GetTypeByMetadataName(candidate),
                        ref match,
                        ref ambiguous,
                        ref sawNonException);
                    if (ambiguous)
                    {
                        return ExceptionTypeResolution.Ambiguous;
                    }
                }
            }

            if (match is not null)
            {
                return new ExceptionTypeResolution(match, isAmbiguous: false);
            }

            return sawNonException
                ? ExceptionTypeResolution.NotAnException
                : ExceptionTypeResolution.NotFound;
        }

        private void consider(
            INamedTypeSymbol? candidate,
            ref INamedTypeSymbol? match,
            ref bool ambiguous,
            ref bool sawNonException)
        {
            if (candidate is null || ambiguous)
            {
                return;
            }

            if (!this.IsExceptionType(candidate))
            {
                sawNonException = true;
                return;
            }

            if (match is not null && !SymbolEqualityComparer.Default.Equals(match, candidate))
            {
                ambiguous = true;
                match = null;
                return;
            }

            match = candidate;
        }

        private ExceptionTypeResolution resolveShortName(string name) =>
            this.getShortNames().Resolve(name);

        private ShortNameIndex getShortNames()
        {
            ShortNameIndex? existing = Volatile.Read(ref this.shortNames);
            if (existing is not null)
            {
                return existing;
            }

            var built = new ShortNameIndex(this);
            return Interlocked.CompareExchange(ref this.shortNames, built, null) ?? built;
        }

        private sealed class ShortNameIndex
        {
            private readonly Dictionary<string, INamedTypeSymbol> uniqueExceptions = new(StringComparer.Ordinal);
            private readonly HashSet<string> ambiguous = new(StringComparer.Ordinal);
            private readonly HashSet<string> nonExceptions = new(StringComparer.Ordinal);

            internal ShortNameIndex(ExceptionTypeResolver resolver)
            {
                this.visit(resolver.compilation.GlobalNamespace, resolver);
            }

            internal ExceptionTypeResolution Resolve(string name)
            {
                if (this.ambiguous.Contains(name))
                {
                    return ExceptionTypeResolution.Ambiguous;
                }

                if (this.uniqueExceptions.TryGetValue(name, out INamedTypeSymbol? match))
                {
                    return new ExceptionTypeResolution(match, isAmbiguous: false);
                }

                return this.nonExceptions.Contains(name)
                    ? ExceptionTypeResolution.NotAnException
                    : ExceptionTypeResolution.NotFound;
            }

            private void visit(INamespaceSymbol namespaceSymbol, ExceptionTypeResolver resolver)
            {
                foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
                {
                    this.visit(type, resolver);
                }

                foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
                {
                    this.visit(nestedNamespace, resolver);
                }
            }

            private void visit(INamedTypeSymbol type, ExceptionTypeResolver resolver)
            {
                if (resolver.IsExceptionType(type))
                {
                    this.addException(type);
                }
                else
                {
                    this.nonExceptions.Add(type.Name);
                }

                foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
                {
                    this.visit(nestedType, resolver);
                }
            }

            private void addException(INamedTypeSymbol type)
            {
                if (this.ambiguous.Contains(type.Name))
                {
                    return;
                }

                if (!this.uniqueExceptions.TryGetValue(type.Name, out INamedTypeSymbol? existing))
                {
                    this.uniqueExceptions.Add(type.Name, type);
                    return;
                }

                if (SymbolEqualityComparer.Default.Equals(existing, type))
                {
                    return;
                }

                this.uniqueExceptions.Remove(type.Name);
                this.ambiguous.Add(type.Name);
            }
        }

        internal bool IsExceptionType(ITypeSymbol type) =>
            type is INamedTypeSymbol named &&
            named.TypeKind != TypeKind.Error &&
            this.exceptionBaseType is not null &&
            named.InheritsOrEquals(this.exceptionBaseType);
    }

    internal readonly struct ExceptionTypeResolution
    {
        internal static ExceptionTypeResolution NotFound { get; } = new(null, isAmbiguous: false);

        internal static ExceptionTypeResolution Ambiguous { get; } = new(null, isAmbiguous: true);

        internal static ExceptionTypeResolution NotAnException { get; } = new(null, isAmbiguous: false, isNotAnException: true);

        internal ExceptionTypeResolution(INamedTypeSymbol? type, bool isAmbiguous, bool isNotAnException = false)
        {
            this.Type = type;
            this.IsAmbiguous = isAmbiguous;
            this.IsNotAnException = isNotAnException;
        }

        internal INamedTypeSymbol? Type { get; }

        internal bool IsAmbiguous { get; }

        internal bool IsNotAnException { get; }
    }
}