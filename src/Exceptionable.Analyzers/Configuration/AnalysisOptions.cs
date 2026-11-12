using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Configuration
{
    internal sealed class AnalysisOptions
    {
        internal const string AnalysisKey = "exceptionable_analysis";
        internal const string AnalysisProperty = "build_property.Exceptionable";
        internal const string PolyfillKey = "exceptionable_polyfill";
        internal const string PolyfillProperty = "build_property.ExceptionablePolyfills";

        private readonly AnalyzerConfigOptionsProvider optionsProvider;
        private readonly ConcurrentDictionary<SyntaxTree, bool> enabledByTree = new();

        internal AnalysisOptions(AnalyzerConfigOptionsProvider optionsProvider)
        {
            this.optionsProvider = optionsProvider;
        }

        internal bool IsEnabled(SyntaxTree tree) =>
            this.enabledByTree.GetOrAdd(tree, this.resolveEnabled);

        private bool resolveEnabled(SyntaxTree tree)
        {
            AnalyzerConfigOptions treeOptions = this.optionsProvider.GetOptions(tree);
            if (treeOptions.TryGetValue(AnalysisKey, out var treeValue) &&
                tryParse(treeValue, out var treeEnabled))
            {
                return treeEnabled;
            }

            return this.optionsProvider.GlobalOptions.TryGetValue(
                    AnalysisProperty,
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
                if (treeOptions.TryGetValue(PolyfillKey + "." + scope, out var scopedValue) &&
                    tryParse(scopedValue, out var scopedEnabled))
                {
                    return scopedEnabled;
                }
            }

            if (treeOptions.TryGetValue(PolyfillKey, out var treeValue) &&
                tryParse(treeValue, out var treeEnabled))
            {
                return treeEnabled;
            }

            return !this.optionsProvider.GlobalOptions.TryGetValue(
                    PolyfillProperty,
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