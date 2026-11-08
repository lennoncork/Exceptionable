using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class UnusedDeclaredExceptionTests
    {
        [Test]
        public void DeclaredThrows_ChangedException_ReportsExc002AndExc008()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(DeclaredThrows_ChangedException_ReportsExc002AndExc008)));

            Diagnostic mismatch = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(mismatch.GetMessage(), Does.Contain("System.InvalidOperationException"));
            Diagnostic unused = diagnostics.Single(item => item.Id == "EXC008");
            Assert.That(unused.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(
                unused.GetMessage(),
                Is.EqualTo("Exception type 'System.IO.IOException' is declared by [Throws] on 'M' but does not escape"));
            Assert.That(span(unused), Does.Contain("Throws"));
        }

        [Test]
        public void DeclaredThrows_OneOfTwoBecomesUnused_ReportsExc008()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(DeclaredThrows_OneOfTwoBecomesUnused_ReportsExc008)));

            Diagnostic unused = diagnostics.Single(item => item.Id == "EXC008");
            Assert.That(unused.GetMessage(), Does.Contain("System.IO.IOException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void DeclaredThrows_DerivedThrow_DoesNotReportExc008()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(DeclaredThrows_DerivedThrow_DoesNotReportExc008)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void DeclaredThrows_EmptyBody_DoesNotReportExc008()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(DeclaredThrows_EmptyBody_DoesNotReportExc008)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void DeclaredThrows_PropagatedCall_DoesNotReportExc008()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(DeclaredThrows_PropagatedCall_DoesNotReportExc008)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC008"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        private static string span(Diagnostic diagnostic) =>
            diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString();
    }
}