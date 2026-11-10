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

        internal bool Matches(ExceptionContract other)
        {
            if (this.IsDoesNotThrow != other.IsDoesNotThrow)
            {
                return false;
            }

            return this.IsDoesNotThrow || declaredTypesMatch(this.DeclaredTypes, other.DeclaredTypes);
        }

        private static bool declaredTypesMatch(
            ImmutableArray<INamedTypeSymbol> left,
            ImmutableArray<INamedTypeSymbol> right)
        {
            return containsAll(left, right) && containsAll(right, left);
        }

        private static bool containsAll(
            ImmutableArray<INamedTypeSymbol> source,
            ImmutableArray<INamedTypeSymbol> required)
        {
            foreach (INamedTypeSymbol item in required)
            {
                if (!contains(source, item))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool contains(ImmutableArray<INamedTypeSymbol> types, INamedTypeSymbol type)
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