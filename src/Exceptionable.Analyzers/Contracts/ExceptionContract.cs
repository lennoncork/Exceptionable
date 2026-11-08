using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal readonly struct ExceptionContract
    {
        internal static ExceptionContract Unspecified { get; } =
            new(false, ImmutableArray<INamedTypeSymbol>.Empty);

        internal ExceptionContract(bool isDoesNotThrow, ImmutableArray<INamedTypeSymbol> declaredTypes)
        {
            this.IsDoesNotThrow = isDoesNotThrow;
            this.DeclaredTypes = declaredTypes;
        }

        internal bool IsDoesNotThrow { get; }

        internal ImmutableArray<INamedTypeSymbol> DeclaredTypes { get; }

        internal bool HasContract => this.IsDoesNotThrow || this.DeclaredTypes.Length > 0;
    }
}