using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class CallbackContractTests
    {
        [Test]
        public void CallbackParameter_ThrowingLambda_ReportsExc010()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_ThrowingLambda_ReportsExc010)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC010");
            Assert.That(diagnostic.GetMessage(), Does.Contain("callback"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.InvalidOperationException"));
        }

        [Test]
        public void CallbackParameter_DoesNotThrowLambda_DoesNotReport()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_DoesNotThrowLambda_DoesNotReport)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC010"));
        }

        [Test]
        public void CallbackParameter_MethodGroup_ReportsExc010()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_MethodGroup_ReportsExc010)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC010");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.IO.IOException"));
        }

        [Test]
        public void CallbackParameter_AllowedThrows_DoesNotReport()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_AllowedThrows_DoesNotReport)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void CallbackParameter_DisallowedThrows_ReportsExc010()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_DisallowedThrows_ReportsExc010)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC010"));
        }

        [Test]
        public void CallbackParameter_InvokeDoesNotThrow()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_InvokeDoesNotThrow)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void CallbackParameter_InvokeThrows_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_InvokeThrows_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.IO.IOException"));
        }

        [Test]
        public void CallbackParameter_NonDelegate_ReportsExc011()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_NonDelegate_ReportsExc011)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC011");
            Assert.That(diagnostic.GetMessage(), Does.Contain("value"));
        }

        [Test]
        public void CallbackParameter_UnknownDelegate_DoesNotReport()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_UnknownDelegate_DoesNotReport)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC010"));
        }

        [Test]
        public void TaskMethod_ImmediateThrow_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(TaskMethod_ImmediateThrow_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.InvalidOperationException"));
        }

        [Test]
        public void LocalFunction_CallInsideDoesNotThrow_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(LocalFunction_CallInsideDoesNotThrow_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.InvalidOperationException"));
        }

        [Test]
        public void LocalFunction_DoesNotThrow_ReportsExc001()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(LocalFunction_DoesNotThrow_ReportsExc001)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC001");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.InvalidOperationException"));
            Assert.That(diagnostics.Count(item => item.Id == "EXC001"), Is.EqualTo(1));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void CallbackParameter_InvokeDoesNotThrow_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(CallbackParameter_InvokeDoesNotThrow_ReportsExc006)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC006"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void TaskInterface_DeclaredThrows_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(TaskInterface_DeclaredThrows_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }
    }
}
