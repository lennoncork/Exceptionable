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

        internal static string GetContractDisplayName(this IMethodSymbol method)
        {
            var memberName = method.AssociatedSymbol?.Name;
            return method.MethodKind switch
            {
                MethodKind.StaticConstructor => $"{method.ContainingType.Name} static constructor",
                MethodKind.Constructor => $"{method.ContainingType.Name} constructor",
                MethodKind.PropertyGet => $"{memberName ?? method.Name} getter",
                MethodKind.PropertySet => $"{memberName ?? method.Name} setter",
                MethodKind.EventAdd => $"{memberName ?? method.Name} add accessor",
                MethodKind.EventRemove => $"{memberName ?? method.Name} remove accessor",
                _ => method.Name,
            };
        }
    }
}