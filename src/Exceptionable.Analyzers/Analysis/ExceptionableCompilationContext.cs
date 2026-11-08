using System.Collections.Concurrent;
using Exceptionable.Analyzers.Configuration;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class ExceptionableCompilationContext
    {
        private readonly ConcurrentDictionary<IMethodSymbol, bool> supportedMethods =
            new(SymbolEqualityComparer.Default);

        internal ExceptionableCompilationContext(
            Compilation compilation,
            AnalyzerConfigOptionsProvider optionsProvider)
        {
            this.Compilation = compilation;
            this.Options = new AnalysisOptions(optionsProvider);
            this.ExceptionTypeResolver = new ExceptionTypeResolver(compilation);
            this.ContractResolver = new ContractResolver(this.ExceptionTypeResolver);
            this.Reachability = new ReachabilityAnalysis();
        }

        internal bool IsSupportedMethod(IMethodSymbol method) =>
            this.supportedMethods.GetOrAdd(method, static candidate => MethodAnalysisPolicy.IsSupportedMethod(candidate));

        internal Compilation Compilation { get; }

        internal AnalysisOptions Options { get; }

        internal ExceptionTypeResolver ExceptionTypeResolver { get; }

        internal ContractResolver ContractResolver { get; }

        internal ReachabilityAnalysis Reachability { get; }
    }
}