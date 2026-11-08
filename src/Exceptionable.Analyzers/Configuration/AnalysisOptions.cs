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
            AnalyzerConfigOptions treeOptions = this.optionsProvider.GetOptions(tree);
            if (treeOptions.TryGetValue("exceptionable_analysis", out var treeValue) &&
                tryParse(treeValue, out var treeEnabled))
            {
                return treeEnabled;
            }

            return this.optionsProvider.GlobalOptions.TryGetValue(
                    "build_property.Exceptionable",
                    out var globalValue) &&
                string.Equals(globalValue, "enable", StringComparison.OrdinalIgnoreCase);
        }

        private static bool tryParse(string value, out bool enabled)
        {
            if (string.Equals(value, "enable", StringComparison.OrdinalIgnoreCase))
            {
                enabled = true;
                return true;
            }

            if (string.Equals(value, "disable", StringComparison.OrdinalIgnoreCase))
            {
                enabled = false;
                return true;
            }

            enabled = false;
            return false;
        }
    }
}