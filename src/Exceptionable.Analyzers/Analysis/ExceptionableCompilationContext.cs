using System.Collections.Concurrent;
using Exceptionable.Analyzers.Configuration;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Polyfills;
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
            this.Polyfills = new PolyfillResolver(compilation, PolyfillCatalog.Modules);
            this.ExceptionTypeResolver = new ExceptionTypeResolver(compilation);
            this.Reachability = new ReachabilityAnalysis();
            this.InferredThrows = new InferredThrowAnalysis(compilation, this.Reachability, this.ExceptionTypeResolver, this.IsSupportedMethod);
            this.ContractResolver = new ContractResolver(
                this.ExceptionTypeResolver,
                this.InferredThrows);
        }

        internal bool IsSupportedMethod(IMethodSymbol method) =>
            this.supportedMethods.GetOrAdd(method, static candidate => MethodAnalysisPolicy.IsSupportedMethod(candidate));

        internal Compilation Compilation { get; }

        internal AnalysisOptions Options { get; }

        internal PolyfillResolver Polyfills { get; }

        internal ExceptionTypeResolver ExceptionTypeResolver { get; }

        internal ContractResolver ContractResolver { get; }

        internal ReachabilityAnalysis Reachability { get; }

        internal InferredThrowAnalysis InferredThrows { get; }
    }
}