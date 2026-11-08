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
        private readonly ConcurrentDictionary<string, ResolvedType> cache =
            new(StringComparer.Ordinal);

        internal ExceptionTypeResolver(Compilation compilation)
        {
            this.compilation = compilation;
            this.exceptionBaseType = compilation.GetTypeByMetadataName("System.Exception");
        }

        internal INamedTypeSymbol? ResolveExceptionType(string value)
        {
            var trimmed = value.Trim();
            return this.cache.GetOrAdd(trimmed, this.resolveUncached).Type;
        }

        private ResolvedType resolveUncached(string trimmed)
        {
            if (string.Equals(trimmed, "Exception", StringComparison.Ordinal))
            {
                return new ResolvedType(this.exceptionBaseType);
            }

            var resolved = trimmed.IndexOf('.') >= 0 || trimmed.IndexOf('+') >= 0
                ? this.resolveMetadataName(trimmed)
                : this.resolveShortName(trimmed);
            return new ResolvedType(resolved);
        }

        private INamedTypeSymbol? resolveMetadataName(string name)
        {
            var exact = this.compilation.GetTypeByMetadataName(name);
            if (exact is not null)
            {
                return this.isException(exact) ? exact : null;
            }

            if (name.IndexOf('+') >= 0 || name.IndexOf('.') < 0)
            {
                return null;
            }

            INamedTypeSymbol? match = null;
            var parts = name.Split('.');
            for (var namespaceLength = parts.Length - 1; namespaceLength >= 1; namespaceLength--)
            {
                var candidate = string.Join(".", parts, 0, namespaceLength) + "+" +
                    string.Join("+", parts, namespaceLength, parts.Length - namespaceLength);
                var resolved = this.compilation.GetTypeByMetadataName(candidate);
                if (resolved is null || !this.isException(resolved))
                {
                    continue;
                }

                if (match is not null && !SymbolEqualityComparer.Default.Equals(match, resolved))
                {
                    return null;
                }

                match = resolved;
            }

            return match;
        }

        private INamedTypeSymbol? resolveShortName(string name)
        {
            INamedTypeSymbol? match = null;
            return this.tryFind(this.compilation.GlobalNamespace, name, ref match) ? match : null;
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

        private sealed class ResolvedType
        {
            internal ResolvedType(INamedTypeSymbol? type)
            {
                this.Type = type;
            }

            internal INamedTypeSymbol? Type { get; }
        }
    }
}