using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class ThrowContractAnalysis
    {
        internal static void Analyze(
            OperationAnalysisContext operationContext,
            ExceptionableCompilationContext compilationContext)
        {
            if (operationContext.Operation is not IThrowOperation throwOperation ||
                !compilationContext.Options.IsEnabled(throwOperation.Syntax.SyntaxTree) ||
                operationContext.ContainingSymbol is not IMethodSymbol containingMethod ||
                !MethodAnalysisPolicy.IsSupportedMethod(containingMethod) ||
                !compilationContext.Reachability.IsReachable(throwOperation, containingMethod))
            {
                return;
            }

            var exceptionType = throwOperation.Exception is null
                ? null
                : ExceptionFlowAnalysis.GetThrownExceptionType(throwOperation.Exception);
            if (exceptionType is null)
            {
                exceptionType = ExceptionFlowAnalysis.GetRethrownExceptionType(
                    throwOperation,
                    compilationContext.Compilation);
            }

            var exceptionBaseType = compilationContext.Compilation.GetTypeByMetadataName("System.Exception");
            if (exceptionType is null ||
                exceptionBaseType is null ||
                !exceptionType.InheritsOrEquals(exceptionBaseType))
            {
                return;
            }

            var contract = compilationContext.ContractResolver.GetEffectiveContract(containingMethod);
            var interfaceContracts =
                compilationContext.ContractResolver.GetImplementedInterfaceContracts(containingMethod);
            if ((!contract.HasContract && interfaceContracts.Length == 0) ||
                ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType))
            {
                return;
            }

            if (contract.IsDoesNotThrow || interfaceContracts.Any(interfaceContract => interfaceContract.IsDoesNotThrow))
            {
                operationContext.ReportDiagnostic(Diagnostic.Create(
                    ExceptionableDiagnostics.DoesNotThrowEscapes,
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.Name));
            }
            else if ((contract.HasContract &&
                        !contract.DeclaredTypes.Any(declaredType => exceptionType.InheritsOrEquals(declaredType))) ||
                interfaceContracts.Any(interfaceContract =>
                    !interfaceContract.IsDoesNotThrow &&
                    !interfaceContract.DeclaredTypes.Any(declaredType => exceptionType.InheritsOrEquals(declaredType))))
            {
                operationContext.ReportDiagnostic(Diagnostic.Create(
                    ExceptionableDiagnostics.ThrowsMismatch,
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.Name));
            }
        }
    }
}