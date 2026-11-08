using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class CallContractAnalysis
    {
        internal static void Analyze(
            OperationAnalysisContext operationContext,
            ExceptionableCompilationContext compilationContext)
        {
            var operation = operationContext.Operation;
            if (!compilationContext.Options.IsEnabled(operation.Syntax.SyntaxTree) ||
                operationContext.ContainingSymbol is not IMethodSymbol callerMethod ||
                !compilationContext.IsSupportedMethod(callerMethod) ||
                !compilationContext.Reachability.IsReachable(operation, callerMethod))
            {
                return;
            }

            var contractResolver = compilationContext.ContractResolver;
            foreach (var targetMethod in OperationTargetResolver.GetTargetMethods(operation))
            {
                if ((MethodAnalysisPolicy.IsDispatchUncertain(targetMethod) &&
                        targetMethod.ContainingType.TypeKind != TypeKind.Interface) ||
                    !compilationContext.IsSupportedMethod(targetMethod) ||
                    MethodAnalysisPolicy.IsDeferredReturnType(targetMethod.ReturnType))
                {
                    continue;
                }

                var targetContract = contractResolver.GetEffectiveContract(targetMethod);
                foreach (var declaredType in targetContract.DeclaredTypes)
                {
                    if (contractResolver.IsExceptionPermitted(callerMethod, declaredType) ||
                        ExceptionFlowAnalysis.IsCaught(operation, declaredType))
                    {
                        continue;
                    }

                    operationContext.ReportDiagnostic(Diagnostic.Create(
                        ExceptionableDiagnostics.CallerViolation,
                        operation.Syntax.GetLocation(),
                        targetMethod.GetContractDisplayName(),
                        declaredType.Name));
                }
            }
        }
    }
}