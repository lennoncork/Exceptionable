using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Exceptionable.Analyzers.Contracts;
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
        private readonly ConcurrentDictionary<IMethodSymbol, NoThrowMatch> noThrowMatches =
            new(SymbolEqualityComparer.Default);

        internal PolyfillResolver(Compilation compilation, IEnumerable<PolyfillModule> modules)
        {
            this.compilation = compilation;
            foreach (PolyfillModule module in modules)
            {
                foreach (PolyfillApi api in module.Apis)
                {
                    string key = module.Namespace + "." + api.TypeName;
                    if (!this.apisByNamespaceAndType.TryGetValue(module.Namespace, out Dictionary<string, List<PolyfillApi>>? types))
                    {
                        types = new Dictionary<string, List<PolyfillApi>>(StringComparer.Ordinal);
                        this.apisByNamespaceAndType[module.Namespace] = types;
                    }

                    if (!types.TryGetValue(key, out List<PolyfillApi>? apis))
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

        /// <summary>
        /// Gets the catalog namespace when <paramref name="method"/> is a no-throw call: the catalogued
        /// member, or a same-assembly override or interface implementation that is not declared in source.
        /// A source override publishes <c>[DoesNotThrow]</c> only when it declares that contract.
        /// </summary>
        internal bool TryGetCallerDoesNotThrowNamespace(IMethodSymbol method, out string @namespace)
        {
            NoThrowMatch match = this.matchNoThrow(method);
            @namespace = match.Namespace;
            return match.Caller;
        }

        /// <summary>
        /// Gets the catalog namespace when a source member must not let an exception escape because
        /// it overrides or implements a no-throw entry.
        /// </summary>
        internal bool TryGetImplementationConstraintNamespace(IMethodSymbol method, out string @namespace)
        {
            NoThrowMatch match = this.matchNoThrow(method);
            @namespace = match.Namespace;
            return match.Implementation;
        }

        /// <summary>Gets the catalogued member a source override or implementation must not throw from.</summary>
        internal IMethodSymbol? GetImplementationAnchor(IMethodSymbol method)
        {
            NoThrowMatch match = this.matchNoThrow(method);
            return match.Implementation ? match.Anchor : null;
        }

        private NoThrowMatch matchNoThrow(IMethodSymbol method)
        {
            if (!isNoThrowCandidate(method))
            {
                return NoThrowMatch.None;
            }

            IMethodSymbol definition = (method.ReducedFrom ?? method).OriginalDefinition;
            return this.noThrowMatches.GetOrAdd(definition, this.matchNoThrowDefinition);
        }

        private NoThrowMatch matchNoThrowDefinition(IMethodSymbol definition)
        {
            if (!this.tryFindAnchor(definition, out IMethodSymbol? anchor) || anchor is null)
            {
                return NoThrowMatch.None;
            }

            bool source = !definition.DeclaringSyntaxReferences.IsEmpty;
            bool sameAssembly = SymbolEqualityComparer.Default.Equals(
                definition.ContainingAssembly,
                anchor.ContainingAssembly);
            if (!source && !sameAssembly)
            {
                return NoThrowMatch.None;
            }

            string @namespace = anchor.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            // A source member is a no-throw call only after it declares [DoesNotThrow]. The anchor
            // still constrains its body, which is the implementation flag.
            return new NoThrowMatch(caller: !source, implementation: source, @namespace, anchor);
        }

        private bool tryFindAnchor(IMethodSymbol definition, out IMethodSymbol? anchor)
        {
            anchor = null;
            if (this.getDirect(definition) is PolyfillApi direct)
            {
                if (!direct.DoesNotThrow || !definition.DeclaringSyntaxReferences.IsEmpty)
                {
                    return false;
                }

                anchor = definition;
                return true;
            }

            for (IMethodSymbol? current = definition.OverriddenMethod; current is not null; current = current.OverriddenMethod)
            {
                IMethodSymbol candidate = (current.ReducedFrom ?? current).OriginalDefinition;
                if (this.getDirect(candidate) is not PolyfillApi api)
                {
                    continue;
                }

                if (!api.DoesNotThrow)
                {
                    return false;
                }

                anchor = candidate;
                return true;
            }

            INamedTypeSymbol? containingType = definition.ContainingType;
            if (containingType is null)
            {
                return false;
            }

            foreach (INamedTypeSymbol iface in containingType.AllInterfaces)
            {
                foreach (string memberName in memberNames(definition))
                {
                    foreach (ISymbol member in iface.GetMembers(memberName))
                    {
                        if (member is not IMethodSymbol interfaceMethod ||
                            containingType.FindImplementationForInterfaceMember(interfaceMethod) is not IMethodSymbol implementation ||
                            !implementation.IsSameMethod(definition))
                        {
                            continue;
                        }

                        IMethodSymbol candidate = (interfaceMethod.ReducedFrom ?? interfaceMethod).OriginalDefinition;
                        if (this.getDirect(candidate) is not PolyfillApi api || !api.DoesNotThrow)
                        {
                            continue;
                        }

                        anchor = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private PolyfillApi? getDirect(IMethodSymbol definition)
        {
            if (definition.ContainingType is null)
            {
                return null;
            }

            string @namespace = definition.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!this.apisByNamespaceAndType.TryGetValue(@namespace, out Dictionary<string, List<PolyfillApi>>? types))
            {
                return null;
            }

            string key = @namespace + "." + definition.ContainingType.MetadataName;
            if (!types.TryGetValue(key, out List<PolyfillApi>? apis))
            {
                return null;
            }

            foreach (PolyfillApi candidate in apis)
            {
                if (candidate.MemberName == definition.Name &&
                    (candidate.ParameterCount is null || candidate.ParameterCount == definition.Parameters.Length))
                {
                    return candidate;
                }
            }

            return null;
        }

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
            if (!this.apisByNamespaceAndType[@namespace].TryGetValue(key, out List<PolyfillApi>? apis))
            {
                return ImmutableArray<INamedTypeSymbol>.Empty;
            }

            ImmutableArray<INamedTypeSymbol>.Builder types = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (PolyfillApi api in apis)
            {
                if (api.DoesNotThrow ||
                    api.MemberName != definition.Name ||
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

        private static bool isNoThrowCandidate(IMethodSymbol method)
        {
            if (isNoThrowName(method.Name))
            {
                return true;
            }

            // An explicit implementation's Name is qualified, such as
            // System.IComparable<System.Int32>.CompareTo. The interface member keeps the short name.
            foreach (IMethodSymbol implemented in method.ExplicitInterfaceImplementations)
            {
                if (isNoThrowName(implemented.Name))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> memberNames(IMethodSymbol method)
        {
            yield return method.Name;
            foreach (IMethodSymbol implemented in method.ExplicitInterfaceImplementations)
            {
                yield return implemented.Name;
            }
        }

        private static bool isNoThrowName(string name) =>
            name is "Equals" or "GetHashCode" or "ToString" or "CompareTo" or "ReferenceEquals";

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

        private readonly struct NoThrowMatch
        {
            internal static NoThrowMatch None { get; } = new(false, false, string.Empty, null);

            internal NoThrowMatch(bool caller, bool implementation, string @namespace, IMethodSymbol? anchor)
            {
                this.Caller = caller;
                this.Implementation = implementation;
                this.Namespace = @namespace;
                this.Anchor = anchor;
            }

            internal bool Caller { get; }

            internal bool Implementation { get; }

            internal string Namespace { get; }

            internal IMethodSymbol? Anchor { get; }
        }
    }
}