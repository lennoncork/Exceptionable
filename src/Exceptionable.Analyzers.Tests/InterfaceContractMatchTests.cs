using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class InterfaceContractMatchTests
    {
        [Test]
        public void InterfaceContractMismatch_MissingThrows_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_MissingThrows_ReportsExc009)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC009");
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo("The exception contract of 'Implementation.Execute' does not match implemented member 'IApi.Execute'"));
            Assert.That(span(diagnostic), Is.EqualTo("Execute"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void InterfaceContractMismatch_MissingDoesNotThrow_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_MissingDoesNotThrow_ReportsExc009)));

            Diagnostic diagnostic = diagnostics.Single();
            Assert.That(diagnostic.Id, Is.EqualTo("EXC009"));
            Assert.That(
                diagnostic.GetMessage(),
                Does.Contain("Implementation.Execute").And.Contain("IApi.Execute"));
        }

        [Test]
        public void InterfaceContractMismatch_MatchingThrows_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_MatchingThrows_DoesNotReportExc009)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void InterfaceContractMismatch_MatchingDoesNotThrow_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_MatchingDoesNotThrow_DoesNotReportExc009)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void InterfaceContractMismatch_ClassLevelThrows_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_ClassLevelThrows_DoesNotReportExc009)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void InterfaceContractMismatch_DifferentThrows_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_DifferentThrows_ReportsExc009)));

            Assert.That(diagnostics.Select(item => item.Id), Is.EqualTo(new[] { "EXC009" }));
        }

        [Test]
        public void InterfaceContractMismatch_ThrowsVersusDoesNotThrow_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_ThrowsVersusDoesNotThrow_ReportsExc009)));

            Assert.That(diagnostics.Select(item => item.Id), Is.EqualTo(new[] { "EXC009" }));
        }

        [Test]
        public void InterfaceContractMismatch_ExplicitImplementation_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_ExplicitImplementation_ReportsExc009)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC009");
            Assert.That(span(diagnostic), Is.EqualTo("Execute"));
        }

        [Test]
        public void InterfaceContractMismatch_Property_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_Property_ReportsExc009)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC009");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Implementation.Value getter"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("IApi.Value getter"));
            Assert.That(span(diagnostic), Is.EqualTo("Value"));
        }

        [Test]
        public void InterfaceContractMismatch_MatchingProperty_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_MatchingProperty_DoesNotReportExc009)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void InterfaceContractMismatch_UnannotatedInterface_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_UnannotatedInterface_DoesNotReportExc009)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC009"));
        }

        [Test]
        public void InterfaceContractMismatch_InheritedInterfaceMember_ReportsExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_InheritedInterfaceMember_ReportsExc009)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC009");
            Assert.That(diagnostic.GetMessage(), Does.Contain("IBaseApi.Execute"));
        }

        [Test]
        public void InterfaceContractMismatch_MultipleInterfaces_ReportsEachMismatch()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_MultipleInterfaces_ReportsEachMismatch)));

            string[] messages = diagnostics
                .Where(item => item.Id == "EXC009")
                .Select(item => item.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("IRead.Execute"));
            Assert.That(messages, Has.Some.Contains("IWrite.Execute"));
        }

        [Test]
        public void InterfaceContractMismatch_AnalysisDisabled_DoesNotReportExc009()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractMismatch_AnalysisDisabled_DoesNotReportExc009)),
                projectSetting: "disable",
                fileSetting: "disable");

            Assert.That(diagnostics, Is.Empty);
        }

        private static string span(Diagnostic diagnostic) =>
            diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString();
    }
}