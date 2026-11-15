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
                !compilationContext.Options.IsEnabled(throwOperation.Syntax.SyntaxTree))
            {
                return;
            }

            if (!compilationContext.Reachability.IsReachable(throwOperation))
            {
                return;
            }

            IMethodSymbol[] hosts = [.. ExecutableHostResolver.GetAnalysisHosts(
                throwOperation,
                operationContext.ContainingSymbol,
                compilationContext.IsSupportedMethod)];
            if (hosts.Length == 0 || hosts.All(host => OperationScope.IsHiddenFrom(throwOperation, host)))
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
            if (violatesNoThrow(hosts, throwOperation, compilationContext) ||
                applicable.Any(contractResolver.IsEffectivelyDoesNotThrow))
            {
                operationContext.ReportDiagnostic(DiagnosticFactory.DoesNotThrowEscapes(
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.GetExceptionDisplayName()));
            }
            else if (!redundantArgumentNull &&
                applicable.Length > 0 &&
                applicable.Any(host => !contractResolver.IsExceptionPermitted(host, exceptionType)))
            {
                operationContext.ReportDiagnostic(DiagnosticFactory.ThrowsMismatch(
                    throwOperation.Syntax.GetLocation(),
                    exceptionType.GetExceptionDisplayName()));
            }
        }

        /// <summary>
        /// A base <c>[DoesNotThrow]</c> or a no-throw polyfill still reports when the member has not
        /// declared <c>[DoesNotThrow]</c>. An explicit <c>[Throws]</c> does not remove that requirement.
        /// An inferred throw stays visible to callers, so it does not satisfy this check.
        /// </summary>
        private static bool violatesNoThrow(
            IMethodSymbol[] hosts,
            IThrowOperation throwOperation,
            ExceptionableCompilationContext compilationContext)
        {
            ContractResolver contractResolver = compilationContext.ContractResolver;
            foreach (IMethodSymbol host in hosts)
            {
                if (contractResolver.IsEffectivelyDoesNotThrow(host))
                {
                    continue;
                }

                if (contractResolver.GetDoesNotThrowBaseMethod(host) is not null)
                {
                    return true;
                }

                if (compilationContext.Polyfills.TryGetImplementationConstraintNamespace(host, out string @namespace) &&
                    compilationContext.Options.IsPolyfillEnabled(throwOperation.Syntax.SyntaxTree, @namespace))
                {
                    return true;
                }
            }

            return false;
        }
    }
}