using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal sealed class ContractResolver
    {
        private readonly ExceptionTypeResolver exceptionTypeResolver;
        private readonly IImplicitContractSource? implicitContracts;
        private readonly ConcurrentDictionary<IMethodSymbol, ExceptionContract> declaredContracts =
            new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<IMethodSymbol, ExceptionContract> effectiveContracts =
            new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<IMethodSymbol>> implementedInterfaceMembers =
            new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<ExceptionContract>> implementedInterfaceContracts =
            new(SymbolEqualityComparer.Default);

        internal ContractResolver(
            ExceptionTypeResolver exceptionTypeResolver,
            IImplicitContractSource? implicitContracts = null)
        {
            this.exceptionTypeResolver = exceptionTypeResolver;
            this.implicitContracts = implicitContracts;
        }

        internal ExceptionContract GetDeclaredContract(IMethodSymbol method) =>
            this.declaredContracts.GetOrAdd(method, this.resolveDeclaredContract);

        internal ExceptionContract GetEffectiveContract(IMethodSymbol method) =>
            this.effectiveContracts.GetOrAdd(method, this.resolveEffectiveContract);

        internal ImmutableArray<IMethodSymbol> GetImplementedInterfaceMembers(IMethodSymbol method) =>
            this.implementedInterfaceMembers.GetOrAdd(method, this.resolveImplementedInterfaceMembers);

        internal ImmutableArray<ExceptionContract> GetImplementedInterfaceContracts(IMethodSymbol method) =>
            this.implementedInterfaceContracts.GetOrAdd(method, this.resolveImplementedInterfaceContracts);

        internal bool HasApplicableContract(IMethodSymbol method) =>
            this.GetEffectiveContract(method).HasContract ||
            this.GetImplementedInterfaceContracts(method).Length > 0;

        internal bool IsEffectivelyDoesNotThrow(IMethodSymbol method) =>
            this.GetEffectiveContract(method).IsDoesNotThrow ||
            this.GetImplementedInterfaceContracts(method).Any(contract => contract.IsDoesNotThrow);

        /// <summary>
        /// A base method <c>[DoesNotThrow]</c> is a requirement on each override. It is not the
        /// override's published contract until the override, its property or event, or a type in
        /// its hierarchy declares <c>[DoesNotThrow]</c>.
        /// </summary>
        internal IMethodSymbol? GetDoesNotThrowBaseMethod(IMethodSymbol method)
        {
            for (IMethodSymbol? current = method.OverriddenMethod; current is not null; current = current.OverriddenMethod)
            {
                if (this.getOwnContract(current).IsDoesNotThrow)
                {
                    return current;
                }
            }

            return null;
        }

        internal bool DeclaresDoesNotThrow(IMethodSymbol method)
        {
            if (this.getOwnContract(method).IsDoesNotThrow)
            {
                return true;
            }

            for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.BaseType)
            {
                ExceptionContract typeContract = this.GetDirectContract(type);
                if (!typeContract.HasContract)
                {
                    continue;
                }

                return typeContract.IsDoesNotThrow;
            }

            return false;
        }

        internal bool IsExceptionPermitted(IMethodSymbol method, ITypeSymbol exceptionType)
        {
            if (this.IsEffectivelyDoesNotThrow(method))
            {
                return false;
            }

            ImmutableArray<ExceptionContract> constraints = this.getDeclaredConstraints(method);
            return constraints.Length > 0 &&
                constraints.All(contract => contract.DeclaredTypes.Any(exceptionType.InheritsOrEquals));
        }

        internal ExceptionContract GetDirectContract(ISymbol symbol)
        {
            ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
            bool doesNotThrow = attributes.Any(ThrowsAttributeReader.IsDoesNotThrow);
            ImmutableArray<INamedTypeSymbol>.Builder declaredTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (AttributeData attribute in attributes.Where(ThrowsAttributeReader.IsThrows))
            {
                if (ThrowsAttributeReader.TryGetThrowsType(attribute, out ITypeSymbol? typeArgument))
                {
                    if (typeArgument is INamedTypeSymbol named &&
                        this.exceptionTypeResolver.IsExceptionType(named))
                    {
                        declaredTypes.Add(named);
                    }

                    continue;
                }

                if (ThrowsAttributeReader.TryGetThrowsName(attribute, out string name))
                {
                    INamedTypeSymbol? resolved = this.exceptionTypeResolver.ResolveExceptionType(name);
                    if (resolved is not null)
                    {
                        declaredTypes.Add(resolved);
                    }
                }
            }

            return doesNotThrow
                ? new ExceptionContract(true, [])
                : declaredTypes.Count > 0
                    ? new ExceptionContract(false, declaredTypes.ToImmutable())
                    : ExceptionContract.Unspecified;
        }

        private ExceptionContract resolveDeclaredContract(IMethodSymbol method) =>
            this.resolveEffectiveContract(method, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));

        private ExceptionContract resolveEffectiveContract(IMethodSymbol method) =>
            this.includeImplicit(method, this.GetDeclaredContract(method));

        private ExceptionContract includeImplicit(IMethodSymbol method, ExceptionContract contract)
        {
            // An explicit [Throws] or [DoesNotThrow] contract replaces inferred throws.
            if (this.implicitContracts is null || contract.HasContract)
            {
                return contract;
            }

            ImmutableArray<INamedTypeSymbol> implicitTypes = this.implicitContracts.GetImplicitDeclaredTypes(method);
            return implicitTypes.IsEmpty
                ? contract
                : new ExceptionContract(false, implicitTypes);
        }

        private ExceptionContract resolveEffectiveContract(IMethodSymbol method, HashSet<IMethodSymbol> seen)
        {
            if (!seen.Add(method))
            {
                return ExceptionContract.Unspecified;
            }

            for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
            {
                ExceptionContract own = this.getOwnContract(current);
                if (!own.HasContract)
                {
                    continue;
                }

                // A base [DoesNotThrow] obligates the override to declare it. It is not inherited
                // as the override's contract. A base [Throws] still applies until the override replaces it.
                if (!SymbolEqualityComparer.Default.Equals(current, method) && own.IsDoesNotThrow)
                {
                    break;
                }

                return own;
            }

            if (method.ContainingType.TypeKind == TypeKind.Interface)
            {
                ExceptionContract inherited = this.resolveRedeclaredInterfaceContract(method, seen);
                if (inherited.HasContract)
                {
                    return inherited;
                }
            }

            for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.BaseType)
            {
                ExceptionContract typeContract = this.GetDirectContract(type);
                if (typeContract.HasContract)
                {
                    return typeContract;
                }
            }

            return ExceptionContract.Unspecified;
        }

        private ExceptionContract getOwnContract(IMethodSymbol method)
        {
            ExceptionContract methodContract = this.GetDirectContract(method);
            if (methodContract.HasContract)
            {
                return methodContract;
            }

            if (method.AssociatedSymbol is ISymbol associated)
            {
                ExceptionContract associatedContract = this.GetDirectContract(associated);
                if (associatedContract.HasContract)
                {
                    return associatedContract;
                }
            }

            return ExceptionContract.Unspecified;
        }

        private ExceptionContract resolveRedeclaredInterfaceContract(
            IMethodSymbol method,
            HashSet<IMethodSymbol> seen)
        {
            ImmutableArray<ExceptionContract>.Builder inherited = ImmutableArray.CreateBuilder<ExceptionContract>();
            foreach (INamedTypeSymbol baseInterface in method.ContainingType.AllInterfaces)
            {
                foreach (IMethodSymbol baseMember in baseInterface.GetMembers(method.Name).OfType<IMethodSymbol>())
                {
                    if (!isInterfaceRedeclaration(method, baseMember))
                    {
                        continue;
                    }

                    ExceptionContract contract = this.resolveEffectiveContract(baseMember, seen);
                    if (contract.HasContract)
                    {
                        inherited.Add(contract);
                    }
                }
            }

            return intersect(inherited.ToImmutable());
        }

        private static bool isInterfaceRedeclaration(IMethodSymbol method, IMethodSymbol baseMember)
        {
            if (method.Name != baseMember.Name ||
                method.Arity != baseMember.Arity ||
                method.Parameters.Length != baseMember.Parameters.Length ||
                method.IsStatic != baseMember.IsStatic ||
                method.MethodKind != baseMember.MethodKind)
            {
                return false;
            }

            for (int index = 0; index < method.Parameters.Length; index++)
            {
                if (method.Parameters[index].RefKind != baseMember.Parameters[index].RefKind ||
                    !typesMatch(method.Parameters[index].Type, baseMember.Parameters[index].Type, method, baseMember))
                {
                    return false;
                }
            }

            return typesMatch(method.ReturnType, baseMember.ReturnType, method, baseMember);
        }

        private static bool typesMatch(
            ITypeSymbol left,
            ITypeSymbol right,
            IMethodSymbol leftMethod,
            IMethodSymbol rightMethod)
        {
            if (left is ITypeParameterSymbol leftParameter &&
                right is ITypeParameterSymbol rightParameter &&
                SymbolEqualityComparer.Default.Equals(leftParameter.DeclaringMethod, leftMethod) &&
                SymbolEqualityComparer.Default.Equals(rightParameter.DeclaringMethod, rightMethod))
            {
                return leftParameter.Ordinal == rightParameter.Ordinal;
            }

            return SymbolEqualityComparer.Default.Equals(left, right);
        }

        private static ExceptionContract intersect(ImmutableArray<ExceptionContract> contracts)
        {
            if (contracts.IsEmpty)
            {
                return ExceptionContract.Unspecified;
            }

            if (contracts.Length == 1)
            {
                return contracts[0];
            }

            if (contracts.Any(contract => contract.IsDoesNotThrow))
            {
                return new ExceptionContract(true, []);
            }

            var kept = new List<INamedTypeSymbol>();
            foreach (ExceptionContract contract in contracts)
            {
                foreach (INamedTypeSymbol candidate in contract.DeclaredTypes)
                {
                    if (kept.Any(existing => SymbolEqualityComparer.Default.Equals(existing, candidate)))
                    {
                        continue;
                    }

                    if (contracts.All(other => other.DeclaredTypes.Any(candidate.InheritsOrEquals)))
                    {
                        kept.Add(candidate);
                    }
                }
            }

            return kept.Count == 0
                ? new ExceptionContract(true, [])
                : new ExceptionContract(false, [.. kept]);
        }

        private ImmutableArray<IMethodSymbol> resolveImplementedInterfaceMembers(IMethodSymbol method)
        {
            if (method.ContainingType.TypeKind == TypeKind.Interface)
            {
                return [];
            }

            IEnumerable<IMethodSymbol> interfaceMembers = method.ExplicitInterfaceImplementations
                .Concat(method.ContainingType.AllInterfaces.SelectMany(
                    type => type.GetMembers().OfType<IMethodSymbol>()));
            var seenMembers = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            ImmutableArray<IMethodSymbol>.Builder members = ImmutableArray.CreateBuilder<IMethodSymbol>();

            foreach (IMethodSymbol? interfaceMember in interfaceMembers)
            {
                if (!seenMembers.Add(interfaceMember))
                {
                    continue;
                }

                if (method.ContainingType.FindImplementationForInterfaceMember(interfaceMember) is not IMethodSymbol implementation ||
                    !implementation.IsSameMethod(method))
                {
                    continue;
                }

                members.Add(interfaceMember);
            }

            return members.ToImmutable();
        }

        private ImmutableArray<ExceptionContract> resolveImplementedInterfaceContracts(IMethodSymbol method)
        {
            ImmutableArray<ExceptionContract>.Builder contracts = ImmutableArray.CreateBuilder<ExceptionContract>();
            foreach (IMethodSymbol interfaceMember in this.GetImplementedInterfaceMembers(method))
            {
                ExceptionContract contract = this.GetEffectiveContract(interfaceMember);
                if (contract.HasContract)
                {
                    contracts.Add(contract);
                }
            }

            return contracts.ToImmutable();
        }

        private ImmutableArray<ExceptionContract> getDeclaredConstraints(IMethodSymbol method)
        {
            ImmutableArray<ExceptionContract>.Builder constraints = ImmutableArray.CreateBuilder<ExceptionContract>();
            ExceptionContract ownContract = this.GetEffectiveContract(method);
            if (ownContract.HasContract && !ownContract.IsDoesNotThrow)
            {
                constraints.Add(ownContract);
            }

            foreach (ExceptionContract interfaceContract in this.GetImplementedInterfaceContracts(method))
            {
                if (interfaceContract.HasContract && !interfaceContract.IsDoesNotThrow)
                {
                    constraints.Add(interfaceContract);
                }
            }

            return constraints.ToImmutable();
        }
    }
}