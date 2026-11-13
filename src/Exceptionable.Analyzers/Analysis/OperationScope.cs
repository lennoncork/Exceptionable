using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class OperationScope
    {
        internal static bool IsInNameOf(IOperation operation)
        {
            for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current is INameOfOperation)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The nearest local function or lambda that contains <paramref name="operation"/>, if any.
        /// </summary>
        internal static IMethodSymbol? GetEnclosingFunction(IOperation operation)
        {
            for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current is ILocalFunctionOperation localFunction)
                {
                    return localFunction.Symbol;
                }

                if (current is IAnonymousFunctionOperation anonymousFunction)
                {
                    return anonymousFunction.Symbol;
                }
            }

            return null;
        }

        /// <summary>
        /// True when <paramref name="operation"/> belongs to a different local function or lambda than
        /// <paramref name="host"/>, or when it appears inside <c>nameof</c>.
        /// </summary>
        internal static bool IsHiddenFrom(IOperation operation, IMethodSymbol host)
        {
            if (IsInNameOf(operation))
            {
                return true;
            }

            IMethodSymbol? enclosing = GetEnclosingFunction(operation);
            return enclosing is not null && !enclosing.IsSameMethod(host);
        }
    }
}