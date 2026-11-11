using System.Collections.Immutable;
using Exceptionable.Analyzers.Analysis;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers
{
    /// <summary>Analyzes explicitly declared exception contracts.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExceptionableAnalyzer : DiagnosticAnalyzer
    {
        private static readonly IOperationRule[] operationRules =
        [
            new ThrowContractAnalysis(),
            new CallContractAnalysis(),
            new UnnecessaryCatchAnalysis(),
        ];

        private static readonly ISymbolRule[] symbolRules =
        [
            new ContractDeclarationAnalysis(),
            new UnusedDeclaredExceptionAnalysis(),
            new InterfaceContractMatchAnalysis(),
        ];

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ExceptionableDiagnostics.SupportedDiagnostics;

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var compilationContext = new ExceptionableCompilationContext(
                    startContext.Compilation,
                    startContext.Options.AnalyzerConfigOptionsProvider);

                foreach (IOperationRule rule in operationRules)
                {
                    IOperationRule current = rule;
                    startContext.RegisterOperationAction(
                        operationContext => current.Analyze(operationContext, compilationContext),
                        current.Kinds);
                }

                startContext.RegisterSymbolAction(
                    symbolContext =>
                    {
                        foreach (ISymbolRule rule in symbolRules)
                        {
                            rule.Analyze(symbolContext, compilationContext);
                        }
                    },
                    SymbolKind.Method,
                    SymbolKind.NamedType,
                    SymbolKind.Property,
                    SymbolKind.Event);
            });
        }
    }
}