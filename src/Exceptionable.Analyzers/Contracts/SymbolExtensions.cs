using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal static class SymbolExtensions
    {
        internal static bool InheritsOrEquals(this ITypeSymbol actual, ITypeSymbol expected)
        {
            for (ITypeSymbol? current = actual; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, expected))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsSameMethod(this IMethodSymbol first, IMethodSymbol second) =>
            SymbolEqualityComparer.Default.Equals(first, second) ||
            SymbolEqualityComparer.Default.Equals(first.OriginalDefinition, second.OriginalDefinition);
    }
}