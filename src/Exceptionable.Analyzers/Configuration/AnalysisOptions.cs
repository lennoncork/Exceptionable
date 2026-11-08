using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Configuration
{
    internal sealed class AnalysisOptions
    {
        private readonly AnalyzerConfigOptionsProvider optionsProvider;

        internal AnalysisOptions(AnalyzerConfigOptionsProvider optionsProvider)
        {
            this.optionsProvider = optionsProvider;
        }

        internal bool IsEnabled(SyntaxTree tree)
        {
            var treeOptions = this.optionsProvider.GetOptions(tree);
            if (treeOptions.TryGetValue("exceptionable_analysis", out var treeValue))
            {
                return string.Equals(treeValue, "enable", StringComparison.OrdinalIgnoreCase);
            }

            return this.optionsProvider.GlobalOptions.TryGetValue(
                    "build_property.Exceptionable",
                    out var globalValue) &&
                string.Equals(globalValue, "enable", StringComparison.OrdinalIgnoreCase);
        }
    }
}