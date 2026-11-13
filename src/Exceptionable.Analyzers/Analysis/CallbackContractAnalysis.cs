using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>
    /// Checks a delegate argument against the <c>[DoesNotThrow]</c> or <c>[Throws]</c> contract of the
    /// parameter it is passed to.
    /// </summary>
    internal sealed class CallbackContractAnalysis : IOperationRule
    {
        public ImmutableArray<OperationKind> Kinds { get; } = [OperationKind.Argument];

        public void Analyze(
            OperationAnalysisContext operationContext,
            ExceptionableCompilationContext compilationContext)
        {
            if (operationContext.Operation is not IArgumentOperation argument ||
                argument.Parameter is null ||
                !compilationContext.Options.IsEnabled(argument.Syntax.SyntaxTree) ||
                OperationScope.IsInNameOf(argument))
            {
                return;
            }

            IParameterSymbol parameter = argument.Parameter;
            ExceptionContract contract = compilationContext.ContractResolver.GetDirectContract(parameter);
            if (!contract.HasContract || !CallbackTargets.IsDelegateType(parameter.Type))
            {
                return;
            }

            IMethodSymbol? scope = ExecutableHostResolver.GetMethods(operationContext.ContainingSymbol)
                .FirstOrDefault();
            if (scope is null ||
                !CallbackTargets.TryGetEscapingExceptions(
                    argument.Value,
                    scope,
                    compilationContext,
                    out ImmutableArray<INamedTypeSymbol> exceptions))
            {
                return;
            }

            foreach (INamedTypeSymbol exceptionType in exceptions)
            {
                if (allows(contract, exceptionType))
                {
                    continue;
                }

                operationContext.ReportDiagnostic(DiagnosticFactory.CallbackContractViolation(
                    argument.Syntax.GetLocation(),
                    parameter.Name,
                    exceptionType.GetExceptionDisplayName()));
            }
        }

        private static bool allows(ExceptionContract contract, ITypeSymbol exceptionType)
        {
            if (!contract.HasContract || contract.IsDoesNotThrow)
            {
                return false;
            }

            foreach (INamedTypeSymbol declared in contract.DeclaredTypes)
            {
                if (exceptionType.InheritsOrEquals(declared))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
