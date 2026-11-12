using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>A rule that analyzes operations of the kinds it declares.</summary>
    internal interface IOperationRule
    {
        ImmutableArray<OperationKind> Kinds { get; }

        void Analyze(OperationAnalysisContext operationContext, ExceptionableCompilationContext compilationContext);
    }

    /// <summary>A rule that analyzes declared symbols.</summary>
    internal interface ISymbolRule
    {
        void Analyze(SymbolAnalysisContext symbolContext, ExceptionableCompilationContext compilationContext);
    }
}