using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    /// <summary>Supplies the exception types a member is inferred to throw when it has no explicit contract.</summary>
    internal interface IImplicitContractSource
    {
        ImmutableArray<INamedTypeSymbol> GetImplicitDeclaredTypes(IMethodSymbol method);
    }
}