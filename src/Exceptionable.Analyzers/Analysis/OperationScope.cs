using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class OperationScope
    {
        internal static bool IsIgnored(IOperation operation)
        {
            for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current is INameOfOperation or ILocalFunctionOperation or IAnonymousFunctionOperation)
                {
                    return true;
                }
            }

            return false;
        }
    }
}