using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class InterfaceContractMatchAnalysis : ISymbolRule
    {
        public void Analyze(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext)
        {
            switch (symbolContext.Symbol)
            {
                case IMethodSymbol method when method.AssociatedSymbol is null:
                    analyzeMethod(symbolContext, compilationContext, method);
                    break;
                case IPropertySymbol property:
                    analyzeMethod(symbolContext, compilationContext, property.GetMethod);
                    analyzeMethod(symbolContext, compilationContext, property.SetMethod);
                    break;
                case IEventSymbol eventSymbol:
                    analyzeMethod(symbolContext, compilationContext, eventSymbol.AddMethod);
                    analyzeMethod(symbolContext, compilationContext, eventSymbol.RemoveMethod);
                    break;
            }
        }

        private static void analyzeMethod(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext,
            IMethodSymbol? method)
        {
            if (method is null)
            {
                return;
            }

            Location? location = getReportLocation(method, compilationContext);
            if (location is null)
            {
                return;
            }

            ContractResolver contracts = compilationContext.ContractResolver;
            ExceptionContract implementationContract = contracts.GetDeclaredContract(method);
            foreach (IMethodSymbol interfaceMember in contracts.GetImplementedInterfaceMembers(method))
            {
                ExceptionContract interfaceContract = contracts.GetDeclaredContract(interfaceMember);
                if (!interfaceContract.HasContract || implementationContract.Matches(interfaceContract))
                {
                    continue;
                }

                ExceptionContract direct = contracts.GetDirectContract(method);
                bool implementationDeclaresContract = direct.HasContract ||
                    (method.AssociatedSymbol is ISymbol associated &&
                        contracts.GetDirectContract(associated).HasContract);
                symbolContext.ReportDiagnostic(DiagnosticFactory.InterfaceContractMismatch(
                    location,
                    method.GetQualifiedContractDisplayName(),
                    interfaceMember.GetQualifiedContractDisplayName(),
                    implementationDeclaresContract ? null : interfaceContract));
            }
        }

        private static Location? getReportLocation(
            IMethodSymbol method,
            ExceptionableCompilationContext compilationContext)
        {
            ISymbol target = method;
            if (method.AssociatedSymbol is ISymbol associated &&
                !compilationContext.ContractResolver.GetDirectContract(method).HasContract)
            {
                target = associated;
            }

            foreach (Location location in target.Locations)
            {
                if (location.IsInSource &&
                    location.SourceTree is SyntaxTree tree &&
                    compilationContext.Options.IsEnabled(tree))
                {
                    return location;
                }
            }

            return null;
        }
    }
}