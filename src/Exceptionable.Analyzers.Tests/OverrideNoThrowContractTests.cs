using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class OverrideNoThrowContractTests
    {
        [Test]
        public void OverrideToString_MissingDoesNotThrow_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideToString_MissingDoesNotThrow_ReportsExc009)));

            Diagnostic diagnostic = diagnostics.Single();
            Assert.That(diagnostic.Id, Is.EqualTo("EXC009"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Widget.ToString").And.Contain("object.ToString"));
            Assert.That(diagnostic.Properties["ContractKind"], Is.EqualTo("DoesNotThrow"));
        }

        [Test]
        public void OverrideToString_PolyfillDisabled_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideToString_MissingDoesNotThrow_ReportsExc009)),
                additionalOptions: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["exceptionable_polyfill.System"] = "disable",
                });

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void OverrideToString_DeclaresDoesNotThrow_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideToString_DeclaresDoesNotThrow_ReportsExc006)));

            Assert.That(diagnostics.Select(item => item.Id), Is.EqualTo(new[] { "EXC006" }));
        }

        [Test]
        public void ParentClassDoesNotThrow_CoversDerivedMembers()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(ParentClassDoesNotThrow_CoversDerivedMembers)));

            Assert.That(diagnostics.Select(item => item.Id).OrderBy(id => id), Is.EqualTo(new[] { "EXC001", "EXC006" }));
            Assert.That(diagnostics.Single(item => item.Id == "EXC001").GetMessage(), Does.Contain("InvalidOperationException"));
        }

        [Test]
        public void OverrideDoesNotThrow_MissingDeclaration_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideDoesNotThrow_MissingDeclaration_ReportsExc009)));

            string[] messages = [.. diagnostics.Select(item => item.GetMessage())];
            Assert.That(diagnostics.Select(item => item.Id), Is.EqualTo(new[] { "EXC009", "EXC009" }));
            Assert.That(messages, Has.Some.Contain("Derived.Safe"));
            Assert.That(messages, Has.Some.Contain("Base.Safe"));
            Assert.That(messages, Has.Some.Contain("Further.Safe"));
            Assert.That(messages, Has.Some.Contain("Mid.Safe"));
        }

        [Test]
        public void OverrideDoesNotThrow_WithDeclaration_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideDoesNotThrow_WithDeclaration_ReportsExc006)));

            Assert.That(diagnostics.Select(item => item.Id), Is.EqualTo(new[] { "EXC006" }));
        }

        [Test]
        public void OverrideDoesNotThrow_ThrowEscapes_ReportsExc001()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideDoesNotThrow_ThrowEscapes_ReportsExc001)));

            Assert.That(diagnostics.Count(item => item.Id == "EXC001"), Is.EqualTo(2));
            Assert.That(diagnostics.Count(item => item.Id == "EXC009"), Is.EqualTo(2));
            Assert.That(diagnostics, Has.Length.EqualTo(4));
        }

        [Test]
        public void RecordStructToString_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(RecordStructToString_DoesNotReportExc009)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void OverrideToString_AnalysisDisabled_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideToString_MissingDoesNotThrow_ReportsExc009)),
                projectSetting: "disable",
                fileSetting: "disable");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void OverrideProperty_MissingDoesNotThrow_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(OverrideProperty_MissingDoesNotThrow_ReportsExc009)));

            Diagnostic diagnostic = diagnostics.Single();
            Assert.That(diagnostic.Id, Is.EqualTo("EXC009"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("Missing.Value").And.Contain("Base.Value"));
            Assert.That(span(diagnostic), Is.EqualTo("Value"));
        }

        [Test]
        public void ExplicitCompareTo_MissingDoesNotThrow_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(ExplicitCompareTo_MissingDoesNotThrow_ReportsExc009)));

            Assert.That(diagnostics.Select(item => item.Id).OrderBy(id => id), Is.EqualTo(new[] { "EXC001", "EXC009" }));
            Assert.That(
                diagnostics.Single(item => item.Id == "EXC009").GetMessage(),
                Does.Contain("CompareTo").And.Contain("IComparable<T>.CompareTo"));
        }

        [Test]
        public void DerivedTypeThrows_DoesNotInheritParentDoesNotThrow()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(DerivedTypeThrows_DoesNotInheritParentDoesNotThrow)));

            Assert.That(diagnostics.Select(item => item.Id).OrderBy(id => id), Is.EqualTo(new[] { "EXC001", "EXC009" }));
        }

        private static string span(Diagnostic diagnostic) =>
            diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString();
    }
}