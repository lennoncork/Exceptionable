using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Exceptionable.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Exceptionable.Analyzers.Tests
{
    public class CodeFixTests
    {
        [Test]
        public void DuplicatedDiagnosticContract_MatchesAnalyzer()
        {
            System.Type ids = typeof(RemoveStaleThrowsCodeFixProvider).Assembly
                .GetType("Exceptionable.Analyzers.CodeFixes.ExceptionableDiagnosticIds")!;
            string read(string name) => (string)ids.GetField(
                name,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetRawConstantValue()!;

            Assert.That(read("CallerViolation"), Is.EqualTo(Diagnostics.ExceptionableDiagnostics.CallerViolation.Id));
            Assert.That(
                read("UnusedDeclaredException"),
                Is.EqualTo(Diagnostics.ExceptionableDiagnostics.UnusedDeclaredException.Id));
            Assert.That(read("ExceptionTypeProperty"), Is.EqualTo(Diagnostics.DiagnosticFactory.ExceptionTypeProperty));
        }

        [Test]
        public void Exc008_RemovesStaleAttribute()
        {
            string source = AnalyzerTestHarness.GetTestCase("CodeFix_Exc008_Input");
            string expected = AnalyzerTestHarness.GetTestCase("CodeFix_Exc008_Expected");

            string[] results = applyFixes(source, new RemoveStaleThrowsCodeFixProvider(), "EXC008");

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(normalize(results[0]), Is.EqualTo(normalize(expected)));
        }

        [Test]
        public void Exc003_AddsThrowsForEachReportedException()
        {
            string source = AnalyzerTestHarness.GetTestCase("CodeFix_Exc003_Input");

            string[] results = applyFixes(source, new AddThrowsCodeFixProvider(), "EXC003");

            Assert.That(results, Has.Length.EqualTo(2));
            Assert.That(results, Has.Some.Contain("Throws(\"System.IO.IOException\")]"));
            Assert.That(results, Has.Some.Contain("UnauthorizedAccessException"));
            Assert.That(results, Has.All.Contain("string M(string path)"));
        }

        [Test]
        public void Exc003_OffersNoFixWhenMemberDoesNotThrow()
        {
            string source = AnalyzerTestHarness.GetTestCase("CodeFix_Exc003_DoesNotThrow");

            Assert.That(applyFixes(source, new AddThrowsCodeFixProvider(), "EXC003"), Is.Empty);
        }

        [Test]
        public void Exc002_AddsTheEscapingExceptionToTheContract()
        {
            string source = AnalyzerTestHarness.GetTestCase(nameof(Exc002AnalyzerTests.ThrowsMethod_ReportsExc002_WhenTypeEscapes));

            string[] results = applyFixes(source, new AddThrowsCodeFixProvider(), "EXC002");

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0], Does.Contain("Throws(\"System.InvalidOperationException\")"));
            Assert.That(results[0], Does.Contain("Throws(\"System.IO.IOException\")"));
        }

        [Test]
        public void Exc006_RemovesTheUnnecessaryTry()
        {
            string source = AnalyzerTestHarness.GetTestCase("UnnecessaryCatch_SingleStatement_ReportsExc006");

            string[] results = applyFixes(source, new RemoveUnnecessaryCatchCodeFixProvider(), "EXC006");

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0], Does.Contain("Safe();"));
            Assert.That(results[0], Does.Not.Contain("catch ("));
            Assert.That(results[0], Does.Not.Contain("try {"));
        }

        [Test]
        public void Exc009_CopiesTheInterfaceThrowsContract()
        {
            string source = AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMatchTests.InterfaceContractMismatch_MissingThrows_ReportsExc009));

            string[] results = applyFixes(source, new MatchInterfaceContractCodeFixProvider(), "EXC009");

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0], Does.Contain("Throws(\"System.IO.IOException\")"));
            Assert.That(results[0], Does.Contain("public void Execute()"));
            Assert.That(
                results[0].Split(["Throws(\"System.IO.IOException\")"], StringSplitOptions.None),
                Has.Length.EqualTo(3));
        }

        private static string normalize(string text) => text.Replace("\r\n", "\n").TrimEnd();

        private static string[] applyFixes(string source, CodeFixProvider provider, string diagnosticId)
        {
            using AdhocWorkspace workspace = new();
            Project project = workspace.AddProject("Test", LanguageNames.CSharp)
                .WithParseOptions(TestCompilation.ParseOptions)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(
                    ((string)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                        .Split(System.IO.Path.PathSeparator)
                        .Select(path => MetadataReference.CreateFromFile(path)))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(DoesNotThrowAttribute).Assembly.Location));
            Document document = project.AddDocument("Test.cs", SourceText.From(source));
            project = document.Project;

            Compilation compilation = project.GetCompilationAsync().GetAwaiter().GetResult()!;
            ImmutableArray<Diagnostic> diagnostics = new CompilationWithAnalyzers(
                    compilation,
                    [new ExceptionableAnalyzer()],
                    new AnalyzerOptions([], new EnabledOptionsProvider()))
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult()
                .Where(diagnostic => diagnostic.Id == diagnosticId)
                .ToImmutableArray();
            Assert.That(diagnostics, Is.Not.Empty);

            document = project.GetDocument(document.Id)!;
            System.Collections.Generic.List<string> results = [];
            foreach (Diagnostic diagnostic in diagnostics)
            {
                System.Collections.Generic.List<CodeAction> actions = [];
                CodeFixContext context = new(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
                provider.RegisterCodeFixesAsync(context).GetAwaiter().GetResult();
                foreach (CodeAction action in actions)
                {
                    ImmutableArray<CodeActionOperation> operations = action.GetOperationsAsync(CancellationToken.None).GetAwaiter().GetResult();
                    Solution changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
                    results.Add(changed.GetDocument(document.Id)!.GetTextAsync().GetAwaiter().GetResult().ToString());
                }
            }

            return [.. results];
        }

        private sealed class EnabledOptionsProvider : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider
        {
            public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GlobalOptions { get; } = new Options();

            public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Options();

            public override Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Options();

            private sealed class Options : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
            {
                public override bool TryGetValue(string key, out string value)
                {
                    value = key == "exceptionable_analysis" || key == "build_property.Exceptionable" ? "enable" : null!;
                    return value is not null;
                }
            }
        }
    }
}