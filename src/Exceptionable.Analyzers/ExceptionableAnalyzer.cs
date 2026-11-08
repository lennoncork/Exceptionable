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

                startContext.RegisterOperationAction(
                    operationContext => ThrowContractAnalysis.Analyze(operationContext, compilationContext),
                    OperationKind.Throw);

                startContext.RegisterOperationAction(
                    operationContext => CallContractAnalysis.Analyze(operationContext, compilationContext),
                    OperationKind.Invocation,
                    OperationKind.ObjectCreation,
                    OperationKind.PropertyReference,
                    OperationKind.EventAssignment);

                startContext.RegisterSymbolAction(
                    symbolContext => ContractDeclarationAnalysis.Analyze(symbolContext, compilationContext),
                    SymbolKind.Method,
                    SymbolKind.NamedType);
            });
        }
    }
}