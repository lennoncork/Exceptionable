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
        private readonly ConcurrentDictionary<IMethodSymbol, ExceptionContract> effectiveContracts =
            new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<ExceptionContract>> implementedInterfaceContracts =
            new(SymbolEqualityComparer.Default);

        internal ContractResolver(ExceptionTypeResolver exceptionTypeResolver)
        {
            this.exceptionTypeResolver = exceptionTypeResolver;
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

        private ExceptionContract resolveEffectiveContract(IMethodSymbol method)
        {
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