using System.Collections.Generic;
using System.Collections.Immutable;
using Exceptionable.Analyzers.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Tests
{
    public class AnalysisOptionsTests
    {
        [Test]
        public void IsEnabled_UsesProjectSettingWhenNoFileSettingExists()
        {
            var options = new AnalysisOptions(new TestOptionsProvider(
                projectSetting: "ENABLE",
                fileSetting: null));

            Assert.That(options.IsEnabled(CSharpSyntaxTree.ParseText(string.Empty)), Is.True);
        }

        [Test]
        public void IsEnabled_FileSettingOverridesProjectSetting()
        {
            var options = new AnalysisOptions(new TestOptionsProvider(
                projectSetting: "enable",
                fileSetting: "disable"));

            Assert.That(options.IsEnabled(CSharpSyntaxTree.ParseText(string.Empty)), Is.False);
        }

        [Test]
        public void IsEnabled_DefaultsToDisabled()
        {
            var options = new AnalysisOptions(new TestOptionsProvider(
                projectSetting: null,
                fileSetting: null));

            Assert.That(options.IsEnabled(CSharpSyntaxTree.ParseText(string.Empty)), Is.False);
        }

        private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly AnalyzerConfigOptions treeOptions;

            internal TestOptionsProvider(string? projectSetting, string? fileSetting)
            {
                this.GlobalOptions = new TestOptions(
                    "build_property.Exceptionable",
                    projectSetting);
                this.treeOptions = new TestOptions("exceptionable_analysis", fileSetting);
            }

            public override AnalyzerConfigOptions GlobalOptions { get; }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.treeOptions;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.treeOptions;
        }

        private sealed class TestOptions : AnalyzerConfigOptions
        {
            private readonly IReadOnlyDictionary<string, string> values;

            internal TestOptions(string key, string? value)
            {
                this.values = value is null
                    ? ImmutableDictionary<string, string>.Empty
                    : ImmutableDictionary<string, string>.Empty.Add(key, value);
            }

            public override bool TryGetValue(string key, out string value) =>
                this.values.TryGetValue(key, out value!);
        }
    }
}