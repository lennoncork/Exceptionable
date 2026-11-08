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
        private readonly Func<IMethodSymbol, ImmutableArray<INamedTypeSymbol>>? implicitDeclaredTypes;
        private readonly ConcurrentDictionary<IMethodSymbol, ExceptionContract> effectiveContracts =
            new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<ExceptionContract>> implementedInterfaceContracts =
            new(SymbolEqualityComparer.Default);

        internal ContractResolver(
            ExceptionTypeResolver exceptionTypeResolver,
            Func<IMethodSymbol, ImmutableArray<INamedTypeSymbol>>? implicitDeclaredTypes = null)
        {
            this.exceptionTypeResolver = exceptionTypeResolver;
            this.implicitDeclaredTypes = implicitDeclaredTypes;
        }

        internal ExceptionContract GetEffectiveContract(IMethodSymbol method) =>
            this.effectiveContracts.GetOrAdd(method, this.resolveEffectiveContract);

        internal ImmutableArray<ExceptionContract> GetImplementedInterfaceContracts(IMethodSymbol method) =>
            this.implementedInterfaceContracts.GetOrAdd(method, this.resolveImplementedInterfaceContracts);

        internal bool HasApplicableContract(IMethodSymbol method) =>
            this.GetEffectiveContract(method).HasContract ||
            this.GetImplementedInterfaceContracts(method).Length > 0;

        internal bool IsEffectivelyDoesNotThrow(IMethodSymbol method) =>
            this.GetEffectiveContract(method).IsDoesNotThrow ||
            this.GetImplementedInterfaceContracts(method).Any(contract => contract.IsDoesNotThrow);

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

        private ExceptionContract resolveEffectiveContract(IMethodSymbol method) =>
            this.includeImplicit(
                method,
                this.resolveEffectiveContract(method, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default)));

        private ExceptionContract includeImplicit(IMethodSymbol method, ExceptionContract contract)
        {
            if (this.implicitDeclaredTypes is null || contract.IsDoesNotThrow)
            {
                return contract;
            }

            ImmutableArray<INamedTypeSymbol> implicitTypes = this.implicitDeclaredTypes(method);
            if (implicitTypes.IsEmpty)
            {
                return contract;
            }

            if (!contract.HasContract)
            {
                return new ExceptionContract(false, implicitTypes);
            }

            ImmutableArray<INamedTypeSymbol>.Builder declared = contract.DeclaredTypes.ToBuilder();
            foreach (INamedTypeSymbol implicitType in implicitTypes)
            {
                if (declared.Any(declaredType => implicitType.InheritsOrEquals(declaredType)))
                {
                    continue;
                }

                declared.Add(implicitType);
            }

            return declared.Count == contract.DeclaredTypes.Length
                ? contract
                : new ExceptionContract(false, declared.ToImmutable());
        }

        private ExceptionContract resolveEffectiveContract(IMethodSymbol method, HashSet<IMethodSymbol> seen)
        {
            if (!seen.Add(method))
            {
                return ExceptionContract.Unspecified;
            }

            for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
            {
                ExceptionContract methodContract = this.GetDirectContract(current);
                if (methodContract.HasContract)
                {
                    return methodContract;
                }

                if (current.AssociatedSymbol is ISymbol associated)
                {
                    ExceptionContract associatedContract = this.GetDirectContract(associated);
                    if (associatedContract.HasContract)
                    {
                        return associatedContract;
                    }
                }
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

        private ImmutableArray<ExceptionContract> resolveImplementedInterfaceContracts(IMethodSymbol method)
        {
            if (method.ContainingType.TypeKind == TypeKind.Interface)
            {
                return [];
            }

            IEnumerable<IMethodSymbol> interfaceMembers = method.ExplicitInterfaceImplementations
                .Concat(method.ContainingType.AllInterfaces.SelectMany(
                    type => type.GetMembers().OfType<IMethodSymbol>()));
            var seenMembers = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            ImmutableArray<ExceptionContract>.Builder contracts = ImmutableArray.CreateBuilder<ExceptionContract>();

            foreach (IMethodSymbol? interfaceMember in interfaceMembers)
            {
                if (!seenMembers.Add(interfaceMember))
                {
                    continue;
                }

                if (method.ContainingType.FindImplementationForInterfaceMember(interfaceMember) is not IMethodSymbol implementation || !implementation.IsSameMethod(method))
                {
                    continue;
                }

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