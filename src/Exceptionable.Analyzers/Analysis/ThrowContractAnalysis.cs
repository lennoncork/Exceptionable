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
                OperationScope.IsIgnored(throwOperation))
            {
                return;
            }

            IMethodSymbol[] hosts = [.. ConstructorHost.GetMethods(operationContext.ContainingSymbol)
                .Where(compilationContext.IsSupportedMethod)
                .Where(host => compilationContext.Reachability.IsReachable(throwOperation))];
            if (hosts.Length == 0)
            {
                return;
            }

            ITypeSymbol? exceptionType = throwOperation.Exception is null
                ? null
                : ExceptionFlowAnalysis.GetThrownExceptionType(
                    throwOperation.Exception,
                    compilationContext.Compilation);
            exceptionType ??= ExceptionFlowAnalysis.GetRethrownExceptionType(
                    throwOperation,
                    compilationContext.Compilation);

            INamedTypeSymbol? exceptionBaseType = compilationContext.Compilation.GetTypeByMetadataName("System.Exception");
            if (exceptionType is null ||
                exceptionBaseType is null ||
                !exceptionType.InheritsOrEquals(exceptionBaseType) ||
                ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType))
            {
                return;
            }

            bool redundantArgumentNull = compilationContext.InferredThrows
                .IsRedundantNullableGuard(throwOperation, hosts);
            if (redundantArgumentNull)
            {
                IMethodSymbol constructor = hosts.First(host =>
                    host.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor);
                operationContext.ReportDiagnostic(Diagnostic.Create(
                    ExceptionableDiagnostics.RedundantArgumentNullException,
                    throwOperation.Syntax.GetLocation(),
                    constructor.GetContractDisplayName()));
            }

            ContractResolver contractResolver = compilationContext.ContractResolver;
            IMethodSymbol[] applicable = [.. hosts.Where(contractResolver.HasApplicableContract)];
            if (applicable.Length == 0)
            {
                return;
            }

            if (applicable.Any(contractResolver.IsEffectivelyDoesNotThrow))
            {
                operationContext.ReportDiagnostic(Diagnostic.Create(
                    ExceptionableDiagnostics.DoesNotThrowEscapes,
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.GetExceptionDisplayName()));
            }
            else if (!redundantArgumentNull &&
                applicable.Any(host => !contractResolver.IsExceptionPermitted(host, exceptionType)))
            {
                operationContext.ReportDiagnostic(Diagnostic.Create(
                    ExceptionableDiagnostics.ThrowsMismatch,
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.GetExceptionDisplayName()));
            }
        }
    }
}