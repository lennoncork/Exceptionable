using System.Linq;
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
                !MethodAnalysisPolicy.IsSupportedMethod(callerMethod) ||
                !compilationContext.Reachability.IsReachable(operation, callerMethod))
            {
                return;
            }

            var callerContract = compilationContext.ContractResolver.GetEffectiveContract(callerMethod);
            foreach (var targetMethod in OperationTargetResolver.GetTargetMethods(operation))
            {
                if ((MethodAnalysisPolicy.IsDispatchUncertain(targetMethod) &&
                        targetMethod.ContainingType.TypeKind != TypeKind.Interface) ||
                    !MethodAnalysisPolicy.IsSupportedMethod(targetMethod) ||
                    MethodAnalysisPolicy.IsDeferredReturnType(targetMethod.ReturnType))
                {
                    continue;
                }

                var targetContract = compilationContext.ContractResolver.GetEffectiveContract(targetMethod);
                foreach (var declaredType in targetContract.DeclaredTypes)
                {
                    if (callerContract.DeclaredTypes.Any(type => declaredType.InheritsOrEquals(type)) ||
                        ExceptionFlowAnalysis.IsCaught(operation, declaredType))
                    {
                        continue;
                    }

                    operationContext.ReportDiagnostic(Diagnostic.Create(
                        ExceptionableDiagnostics.CallerViolation,
                        operation.Syntax.GetLocation(),
                        targetMethod.Name,
                        declaredType.Name));
                }
            }
        }
    }
}