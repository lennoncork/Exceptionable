using System;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>Decides whether a call target's known contract is enforced at this operation.</summary>
    internal static class CallCheck
    {
        internal static bool IsChecked(
            IMethodSymbol target,
            IOperation operation,
            Func<IMethodSymbol, bool> isSupported,
            ContractResolver contracts,
            bool allowDeferredReturn)
        {
            if (!isSupported(target))
            {
                return false;
            }

            bool uncertain = !MethodAnalysisPolicy.HasCertainDispatch(target, operation);
            if (MethodAnalysisPolicy.IsDeferredReturnType(target.ReturnType) && !allowDeferredReturn)
            {
                // A method with a body throws before it returns, even when the return type is Task
                // or IEnumerable. A signature with no body stays deferred: its contract may describe
                // a fault observed on await or during enumeration.
                if (uncertain || !MethodAnalysisPolicy.HasExecutableBody(target))
                {
                    return false;
                }
            }
            else if (uncertain)
            {
                ExceptionContract declared = contracts.GetDeclaredContract(target);
                if (!declared.HasContract || declared.IsDoesNotThrow)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
