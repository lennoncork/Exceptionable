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

        /// <summary>
        /// Resolves the polyfill setting for a namespace. The most specific
        /// <c>exceptionable_polyfill.&lt;namespace&gt;</c> key wins, then its parent namespaces, then
        /// <c>exceptionable_polyfill</c>, then the <c>ExceptionablePolyfills</c> project property.
        /// Polyfills are enabled when nothing is set.
        /// </summary>
        internal bool IsPolyfillEnabled(SyntaxTree tree, string @namespace)
        {
            AnalyzerConfigOptions treeOptions = this.optionsProvider.GetOptions(tree);
            for (string? scope = @namespace; !string.IsNullOrEmpty(scope); scope = getParentNamespace(scope!))
            {
                if (treeOptions.TryGetValue("exceptionable_polyfill." + scope, out var scopedValue) &&
                    tryParse(scopedValue, out var scopedEnabled))
                {
                    return scopedEnabled;
                }
            }

            if (treeOptions.TryGetValue("exceptionable_polyfill", out var treeValue) &&
                tryParse(treeValue, out var treeEnabled))
            {
                return treeEnabled;
            }

            return !this.optionsProvider.GlobalOptions.TryGetValue(
                    "build_property.ExceptionablePolyfills",
                    out var globalValue) ||
                !string.Equals(globalValue, "disable", StringComparison.OrdinalIgnoreCase);
        }

        private static string? getParentNamespace(string @namespace)
        {
            int separator = @namespace.LastIndexOf('.');
            return separator < 0 ? null : @namespace.Substring(0, separator);
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