using System.Collections.Immutable;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>
    /// Reports EXC009 when a source member must be <c>[DoesNotThrow]</c> and does not declare it.
    /// The requirement comes from a base method marked <c>[DoesNotThrow]</c> or from a no-throw
    /// polyfill the member overrides or implements. Interface contract matching is a separate rule.
    /// </summary>
    internal sealed class NoThrowDeclarationAnalysis : ISymbolRule
    {
        public void Analyze(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext)
        {
            switch (symbolContext.Symbol)
            {
                case IMethodSymbol method when method.AssociatedSymbol is null:
                    analyze(symbolContext, compilationContext, method);
                    break;
                case IPropertySymbol property:
                    analyze(symbolContext, compilationContext, property.GetMethod ?? property.SetMethod);
                    break;
                case IEventSymbol eventSymbol:
                    analyze(symbolContext, compilationContext, eventSymbol.AddMethod ?? eventSymbol.RemoveMethod);
                    break;
            }
        }

        private static void analyze(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext,
            IMethodSymbol? method)
        {
            if (method is null)
            {
                return;
            }

            // Compiler-generated members, such as a record's ToString, have no attribute target of their own.
            ISymbol declared = method.AssociatedSymbol ?? method;
            if (declared.IsImplicitlyDeclared || declared.DeclaringSyntaxReferences.IsEmpty)
            {
                return;
            }

            Location? location = ContractReportLocation.TryGet(method, compilationContext);
            if (location is null)
            {
                return;
            }

            ContractResolver contracts = compilationContext.ContractResolver;
            if (contracts.DeclaresDoesNotThrow(method))
            {
                return;
            }

            IMethodSymbol? required = contracts.GetDoesNotThrowBaseMethod(method);
            if (required is null &&
                location.SourceTree is SyntaxTree tree &&
                compilationContext.Polyfills.TryGetImplementationConstraintNamespace(method, out string @namespace) &&
                compilationContext.Options.IsPolyfillEnabled(tree, @namespace))
            {
                required = compilationContext.Polyfills.GetImplementationAnchor(method);
            }

            if (required is null)
            {
                return;
            }

            // A member that already declares [Throws] cannot also receive [DoesNotThrow] from the fix.
            bool declaresContract = hasDirectContract(contracts, method);
            symbolContext.ReportDiagnostic(DiagnosticFactory.InterfaceContractMismatch(
                location,
                method.GetQualifiedContractDisplayName(),
                required.GetQualifiedContractDisplayName(),
                declaresContract ? null : new ExceptionContract(true, ImmutableArray<INamedTypeSymbol>.Empty)));
        }

        private static bool hasDirectContract(ContractResolver contracts, IMethodSymbol method)
        {
            if (contracts.GetDirectContract(method).HasContract)
            {
                return true;
            }

            return method.AssociatedSymbol is ISymbol associated &&
                contracts.GetDirectContract(associated).HasContract;
        }
    }
}