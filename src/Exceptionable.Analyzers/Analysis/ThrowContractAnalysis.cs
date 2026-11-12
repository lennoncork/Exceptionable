using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class ThrowContractAnalysis : IOperationRule
    {
        public ImmutableArray<OperationKind> Kinds { get; } =
        [
            OperationKind.Throw,
        ];

        public void Analyze(
            OperationAnalysisContext operationContext,
            ExceptionableCompilationContext compilationContext)
        {
            if (operationContext.Operation is not IThrowOperation throwOperation ||
                !compilationContext.Options.IsEnabled(throwOperation.Syntax.SyntaxTree) ||
                OperationScope.IsIgnored(throwOperation))
            {
                return;
            }

            if (!compilationContext.Reachability.IsReachable(throwOperation))
            {
                return;
            }

            IMethodSymbol[] hosts = [.. ExecutableHostResolver.GetMethods(operationContext.ContainingSymbol)
                .Where(compilationContext.IsSupportedMethod)];
            if (hosts.Length == 0)
            {
                return;
            }

            ITypeSymbol? exceptionType = compilationContext.InferredThrows.GetEscapingExceptionType(throwOperation);
            if (exceptionType is null)
            {
                return;
            }

            bool redundantArgumentNull = compilationContext.InferredThrows
                .IsRedundantNullableGuard(throwOperation, hosts);
            if (redundantArgumentNull)
            {
                IMethodSymbol constructor = hosts.First(host =>
                    host.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor);
                operationContext.ReportDiagnostic(DiagnosticFactory.RedundantArgumentNullException(
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
                operationContext.ReportDiagnostic(DiagnosticFactory.DoesNotThrowEscapes(
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.GetExceptionDisplayName()));
            }
            else if (!redundantArgumentNull &&
                applicable.Any(host => !contractResolver.IsExceptionPermitted(host, exceptionType)))
            {
                operationContext.ReportDiagnostic(DiagnosticFactory.ThrowsMismatch(
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.GetExceptionDisplayName()));
            }
        }
    }
}