using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal sealed class ContractResolver
    {
        private readonly ExceptionTypeResolver exceptionTypeResolver;

        internal ContractResolver(ExceptionTypeResolver exceptionTypeResolver)
        {
            this.exceptionTypeResolver = exceptionTypeResolver;
        }

        internal ExceptionContract GetEffectiveContract(IMethodSymbol method)
        {
            for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
            {
                var methodContract = this.GetDirectContract(current);
                if (methodContract.HasContract)
                {
                    return methodContract;
                }
            }

            for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.BaseType)
            {
                var typeContract = this.GetDirectContract(type);
                if (typeContract.HasContract)
                {
                    return typeContract;
                }
            }

            return ExceptionContract.Unspecified;
        }

        internal ImmutableArray<ExceptionContract> GetImplementedInterfaceContracts(IMethodSymbol method)
        {
            if (method.ContainingType.TypeKind == TypeKind.Interface)
            {
                return ImmutableArray<ExceptionContract>.Empty;
            }

            var interfaceMembers = method.ExplicitInterfaceImplementations
                .Concat(method.ContainingType.AllInterfaces.SelectMany(
                    type => type.GetMembers().OfType<IMethodSymbol>()));
            var seenMembers = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var contracts = ImmutableArray.CreateBuilder<ExceptionContract>();

            foreach (var interfaceMember in interfaceMembers)
            {
                if (!seenMembers.Add(interfaceMember))
                {
                    continue;
                }

                var implementation = method.ContainingType.FindImplementationForInterfaceMember(interfaceMember)
                    as IMethodSymbol;
                if (implementation is null || !implementation.IsSameMethod(method))
                {
                    continue;
                }

                var contract = this.GetEffectiveContract(interfaceMember);
                if (contract.HasContract)
                {
                    contracts.Add(contract);
                }
            }

            return contracts.ToImmutable();
        }

        internal ExceptionContract GetDirectContract(ISymbol symbol)
        {
            var attributes = symbol.GetAttributes();
            var doesNotThrow = attributes.Any(ThrowsAttributeReader.IsDoesNotThrow);
            var declaredTypes = attributes
                .Where(ThrowsAttributeReader.IsThrows)
                .Select(ThrowsAttributeReader.GetThrowsName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(this.exceptionTypeResolver.ResolveExceptionType)
                .Where(type => type is not null)
                .Cast<INamedTypeSymbol>()
                .ToImmutableArray();

            return doesNotThrow
                ? new ExceptionContract(true, ImmutableArray<INamedTypeSymbol>.Empty)
                : declaredTypes.Length > 0
                    ? new ExceptionContract(false, declaredTypes)
                    : ExceptionContract.Unspecified;
        }
    }
}