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

        internal static string GetExceptionDisplayName(this ITypeSymbol type) =>
            type.ToDisplayString();

        internal static bool IsSameMethod(this IMethodSymbol first, IMethodSymbol second)
        {
            if (SymbolEqualityComparer.Default.Equals(first, second))
            {
                return true;
            }

            if (!SymbolEqualityComparer.Default.Equals(first.OriginalDefinition, second.OriginalDefinition))
            {
                return false;
            }

            return containingTypesMatch(first.ContainingType, second.ContainingType);
        }

        private static bool containingTypesMatch(INamedTypeSymbol? first, INamedTypeSymbol? second)
        {
            if (first is null || second is null)
            {
                return first is null && second is null;
            }

            if (SymbolEqualityComparer.Default.Equals(first, second))
            {
                return true;
            }

            if (!SymbolEqualityComparer.Default.Equals(first.OriginalDefinition, second.OriginalDefinition))
            {
                return false;
            }

            if (!typeArgumentsMatch(first, second))
            {
                return false;
            }

            return containingTypesMatch(first.ContainingType, second.ContainingType);
        }

        private static bool typeArgumentsMatch(INamedTypeSymbol first, INamedTypeSymbol second)
        {
            if (first.TypeArguments.Length != second.TypeArguments.Length)
            {
                return false;
            }

            for (int index = 0; index < first.TypeArguments.Length; index++)
            {
                ITypeSymbol firstArgument = first.TypeArguments[index];
                ITypeSymbol secondArgument = second.TypeArguments[index];
                if (firstArgument.TypeKind == TypeKind.TypeParameter ||
                    secondArgument.TypeKind == TypeKind.TypeParameter)
                {
                    continue;
                }

                if (!SymbolEqualityComparer.Default.Equals(firstArgument, secondArgument))
                {
                    return false;
                }
            }

            return true;
        }

        internal static string GetQualifiedContractDisplayName(this IMethodSymbol method)
        {
            string typeName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return typeName + "." + method.GetContractDisplayName();
        }

        internal static string GetContractDisplayName(this IMethodSymbol method)
        {
            string? memberName = method.AssociatedSymbol?.Name;
            return method.MethodKind switch
            {
                MethodKind.StaticConstructor => $"{method.ContainingType.Name} static constructor",
                MethodKind.Constructor => $"{method.ContainingType.Name} constructor",
                MethodKind.PropertyGet => describeAccessor(method, memberName, "getter"),
                MethodKind.PropertySet => describeAccessor(method, memberName, "setter"),
                MethodKind.EventAdd => $"{memberName ?? method.Name} add accessor",
                MethodKind.EventRemove => $"{memberName ?? method.Name} remove accessor",
                MethodKind.UserDefinedOperator => describeOperator(method),
                MethodKind.Conversion => method.Name == "op_Implicit"
                    ? "implicit conversion"
                    : "explicit conversion",
                _ => method.Name,
            };
        }

        private static string describeAccessor(IMethodSymbol method, string? memberName, string kind)
        {
            if (method.AssociatedSymbol is IPropertySymbol { IsIndexer: true })
            {
                return "indexer " + kind;
            }

            return $"{memberName ?? method.Name} {kind}";
        }

        private static string describeOperator(IMethodSymbol method)
        {
            string? token = method.Name switch
            {
                "op_Addition" or "op_CheckedAddition" => "+",
                "op_Subtraction" or "op_CheckedSubtraction" => "-",
                "op_Multiply" or "op_CheckedMultiply" => "*",
                "op_Division" or "op_CheckedDivision" => "/",
                "op_Modulus" => "%",
                "op_Equality" => "==",
                "op_Inequality" => "!=",
                "op_GreaterThan" => ">",
                "op_LessThan" => "<",
                "op_GreaterThanOrEqual" => ">=",
                "op_LessThanOrEqual" => "<=",
                "op_BitwiseAnd" => "&",
                "op_BitwiseOr" => "|",
                "op_ExclusiveOr" => "^",
                "op_LeftShift" => "<<",
                "op_RightShift" => ">>",
                "op_UnsignedRightShift" => ">>>",
                "op_UnaryNegation" or "op_CheckedUnaryNegation" => "-",
                "op_UnaryPlus" => "+",
                "op_LogicalNot" => "!",
                "op_OnesComplement" => "~",
                "op_Increment" or "op_CheckedIncrement" => "++",
                "op_Decrement" or "op_CheckedDecrement" => "--",
                "op_True" => "true",
                "op_False" => "false",
                _ => null,
            };
            return token is null ? method.Name : "operator " + token;
        }
    }
}