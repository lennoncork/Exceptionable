using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal sealed class ExceptionTypeResolver
    {
        private readonly Compilation compilation;
        private readonly INamedTypeSymbol? exceptionBaseType;

        internal ExceptionTypeResolver(Compilation compilation)
        {
            this.compilation = compilation;
            this.exceptionBaseType = compilation.GetTypeByMetadataName("System.Exception");
        }

        internal INamedTypeSymbol? ResolveExceptionType(string value)
        {
            var trimmed = value.Trim();
            if (string.Equals(trimmed, "Exception", StringComparison.Ordinal))
            {
                return this.exceptionBaseType;
            }

            INamedTypeSymbol? resolved;
            if (trimmed.IndexOf('.') >= 0)
            {
                resolved = this.compilation.GetTypeByMetadataName(trimmed);
            }
            else
            {
                var matches = findExceptionTypes(this.compilation.GlobalNamespace, trimmed)
                    .Take(2)
                    .ToArray();
                resolved = matches.Length == 1 ? matches[0] : null;
            }

            return resolved is not null &&
                this.exceptionBaseType is not null &&
                resolved.InheritsOrEquals(this.exceptionBaseType)
                    ? resolved
                    : null;
        }

        private static IEnumerable<INamedTypeSymbol> findExceptionTypes(
            INamespaceSymbol namespaceSymbol,
            string name)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                foreach (var match in findExceptionTypes(type, name))
                {
                    yield return match;
                }
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var match in findExceptionTypes(nestedNamespace, name))
                {
                    yield return match;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> findExceptionTypes(INamedTypeSymbol type, string name)
        {
            if (string.Equals(type.Name, name, StringComparison.Ordinal))
            {
                yield return type;
            }

            foreach (var nestedType in type.GetTypeMembers())
            {
                foreach (var match in findExceptionTypes(nestedType, name))
                {
                    yield return match;
                }
            }
        }
    }
}