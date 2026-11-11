using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Polyfills
{
    /// <summary>
    /// Resolves catalog entries to exception symbols for one compilation. It does not decide
    /// whether a namespace is enabled; callers check <c>AnalysisOptions.IsPolyfillEnabled</c>.
    /// </summary>
    internal sealed class PolyfillResolver
    {
        private readonly Compilation compilation;
        private readonly Dictionary<string, Dictionary<string, List<PolyfillApi>>> apisByNamespaceAndType =
            new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<INamedTypeSymbol>> thrownTypes =
            new(SymbolEqualityComparer.Default);

        internal PolyfillResolver(Compilation compilation, IEnumerable<PolyfillModule> modules)
        {
            this.compilation = compilation;
            foreach (PolyfillModule module in modules)
            {
                foreach (PolyfillApi api in module.Apis)
                {
                    string key = module.Namespace + "." + api.TypeName;
                    if (!this.apisByNamespaceAndType.TryGetValue(module.Namespace, out var types))
                    {
                        types = new Dictionary<string, List<PolyfillApi>>(StringComparer.Ordinal);
                        this.apisByNamespaceAndType[module.Namespace] = types;
                    }

                    if (!types.TryGetValue(key, out var apis))
                    {
                        apis = [];
                        types[key] = apis;
                    }

                    apis.Add(api);
                }
            }
        }

        /// <summary>Gets the namespace that holds catalog entries for <paramref name="method"/>, if any.</summary>
        internal string? GetNamespace(IMethodSymbol method)
        {
            if (!method.DeclaringSyntaxReferences.IsEmpty)
            {
                return null;
            }

            string @namespace = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            return this.apisByNamespaceAndType.ContainsKey(@namespace) ? @namespace : null;
        }

        internal ImmutableArray<INamedTypeSymbol> GetThrownTypes(IMethodSymbol method) =>
            this.thrownTypes.GetOrAdd(method, this.resolve);

        private ImmutableArray<INamedTypeSymbol> resolve(IMethodSymbol method)
        {
            IMethodSymbol definition = (method.ReducedFrom ?? method).OriginalDefinition;
            string? @namespace = this.GetNamespace(definition);
            if (@namespace is null ||
                definition.ContainingType is null ||
                definition.ContainingType.ContainingType is not null)
            {
                return ImmutableArray<INamedTypeSymbol>.Empty;
            }

            string key = @namespace + "." + definition.ContainingType.MetadataName;
            if (!this.apisByNamespaceAndType[@namespace].TryGetValue(key, out var apis))
            {
                return ImmutableArray<INamedTypeSymbol>.Empty;
            }

            ImmutableArray<INamedTypeSymbol>.Builder types = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (PolyfillApi api in apis)
            {
                if (api.MemberName != definition.Name ||
                    (api.ParameterCount is int count && count != definition.Parameters.Length))
                {
                    continue;
                }

                foreach (string exception in api.Exceptions)
                {
                    INamedTypeSymbol? type = this.compilation.GetTypeByMetadataName(exception);
                    if (type is not null && !contains(types, type))
                    {
                        types.Add(type);
                    }
                }
            }

            return types.ToImmutable();
        }

        private static bool contains(ImmutableArray<INamedTypeSymbol>.Builder types, INamedTypeSymbol type)
        {
            foreach (INamedTypeSymbol candidate in types)
            {
                if (SymbolEqualityComparer.Default.Equals(candidate, type))
                {
                    return true;
                }
            }

            return false;
        }
    }
}