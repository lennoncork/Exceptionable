using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal sealed class ExceptionTypeResolver
    {
        private readonly Compilation compilation;
        private readonly INamedTypeSymbol? exceptionBaseType;
        private readonly ConcurrentDictionary<string, ExceptionTypeResolution> cache =
            new(StringComparer.Ordinal);

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
            INamedTypeSymbol? exact = this.compilation.GetTypeByMetadataName(name);
            if (exact is not null)
            {
                return this.isException(exact)
                    ? new ExceptionTypeResolution(exact, isAmbiguous: false)
                    : ExceptionTypeResolution.NotFound;
            }

            if (name.IndexOf('+') >= 0 || name.IndexOf('.') < 0)
            {
                return ExceptionTypeResolution.NotFound;
            }

            INamedTypeSymbol? match = null;
            string[] parts = name.Split('.');
            for (int namespaceLength = parts.Length - 1; namespaceLength >= 1; namespaceLength--)
            {
                string candidate = string.Join(".", parts, 0, namespaceLength) + "+" +
                    string.Join("+", parts, namespaceLength, parts.Length - namespaceLength);
                INamedTypeSymbol? resolved = this.compilation.GetTypeByMetadataName(candidate);
                if (resolved is null || !this.isException(resolved))
                {
                    continue;
                }

                if (match is not null && !SymbolEqualityComparer.Default.Equals(match, resolved))
                {
                    return ExceptionTypeResolution.Ambiguous;
                }

                match = resolved;
            }

            return match is null
                ? ExceptionTypeResolution.NotFound
                : new ExceptionTypeResolution(match, isAmbiguous: false);
        }

        private ExceptionTypeResolution resolveShortName(string name)
        {
            INamedTypeSymbol? match = null;
            return this.tryFind(this.compilation.GlobalNamespace, name, ref match)
                ? match is null
                    ? ExceptionTypeResolution.NotFound
                    : new ExceptionTypeResolution(match, isAmbiguous: false)
                : ExceptionTypeResolution.Ambiguous;
        }

        private bool tryFind(INamespaceSymbol namespaceSymbol, string name, ref INamedTypeSymbol? match)
        {
            foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
            {
                if (!this.tryFind(type, name, ref match))
                {
                    return false;
                }
            }

            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                if (!this.tryFind(nestedNamespace, name, ref match))
                {
                    return false;
                }
            }

            return true;
        }

        private bool tryFind(INamedTypeSymbol type, string name, ref INamedTypeSymbol? match)
        {
            if (string.Equals(type.Name, name, StringComparison.Ordinal) && this.isException(type))
            {
                if (match is not null)
                {
                    return false;
                }

                match = type;
            }

            foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
            {
                if (!this.tryFind(nestedType, name, ref match))
                {
                    return false;
                }
            }

            return true;
        }

        private bool isException(INamedTypeSymbol type) =>
            this.exceptionBaseType is not null && type.InheritsOrEquals(this.exceptionBaseType);
    }

    internal readonly struct ExceptionTypeResolution
    {
        internal static ExceptionTypeResolution NotFound { get; } = new(null, isAmbiguous: false);

        internal static ExceptionTypeResolution Ambiguous { get; } = new(null, isAmbiguous: true);

        internal ExceptionTypeResolution(INamedTypeSymbol? type, bool isAmbiguous)
        {
            this.Type = type;
            this.IsAmbiguous = isAmbiguous;
        }

        internal INamedTypeSymbol? Type { get; }

        internal bool IsAmbiguous { get; }
    }
}