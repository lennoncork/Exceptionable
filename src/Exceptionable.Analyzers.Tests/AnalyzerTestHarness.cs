using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Tests
{
    internal static class AnalyzerTestHarness
    {
        internal static Diagnostic[] GetDiagnostics(
            string source,
            string? projectSetting = "enable",
            string? fileSetting = "enable",
            bool includeExceptionableAttributes = true,
            IEnumerable<MetadataReference>? additionalReferences = null,
            string assemblyName = "ExceptionableTests",
            IReadOnlyDictionary<string, string>? additionalOptions = null)
        {
            List<MetadataReference> references = [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))];

            if (includeExceptionableAttributes)
            {
                references.Add(MetadataReference.CreateFromFile(typeof(DoesNotThrowAttribute).Assembly.Location));
            }

            if (additionalReferences is not null)
            {
                references.AddRange(additionalReferences);
            }

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, TestCompilation.ParseOptions);
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            AnalyzerOptions options = new(
                [],
                new TestAnalyzerConfigOptionsProvider(projectSetting, fileSetting, additionalOptions));
            string[] compilerErrors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString())
                .ToArray();
            Assert.That(compilerErrors, Is.Empty, "Analyzer samples must compile.");

            ImmutableArray<Diagnostic> diagnostics = compilation
                .WithAnalyzers([new ExceptionableAnalyzer()], options)
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult();

            return [.. diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden)];
        }

        internal static string GetTestCase(string testName)
        {
            return File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "TestCases",
                $"{testName}.testcase.cs"));
        }

        private sealed class TestAnalyzerConfigOptionsProvider(
            string? projectSetting,
            string? fileSetting,
            IReadOnlyDictionary<string, string>? additionalOptions) : AnalyzerConfigOptionsProvider
        {
            private readonly AnalyzerConfigOptions treeOptions = new TestAnalyzerConfigOptions(
                (additionalOptions ?? ImmutableDictionary<string, string>.Empty)
                    .Where(pair => !pair.Key.StartsWith("build_property.", StringComparison.Ordinal))
                    .Concat(fileSetting is null ? [] : [new("exceptionable_analysis", fileSetting)]));

            public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(
                (additionalOptions ?? ImmutableDictionary<string, string>.Empty)
                    .Where(pair => pair.Key.StartsWith("build_property.", StringComparison.Ordinal))
                    .Concat(projectSetting is null ? [] : [new("build_property.Exceptionable", projectSetting)]));

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.treeOptions;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.treeOptions;
        }

        private sealed class TestAnalyzerConfigOptions(IEnumerable<KeyValuePair<string, string>>? options) : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> values = new(
                options ?? [],
                StringComparer.OrdinalIgnoreCase);

            public override bool TryGetValue(string key, out string value)
            {
                return this.values.TryGetValue(key, out value!);
            }
        }
    }
}
