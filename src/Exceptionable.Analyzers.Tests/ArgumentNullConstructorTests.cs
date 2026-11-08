using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class ArgumentNullConstructorTests
    {
        [Test]
        public void ConstructorArgumentNull_ReportsExc003AtCaller()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ConstructorArgumentNull_ReportsExc003AtCaller)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo("Call to 'Resource constructor' may throw 'System.ArgumentNullException' which is neither caught nor permitted by the caller's contract"));
            Assert.That(span(diagnostic), Does.Contain("new Resource"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void ConstructorArgumentNull_CaughtByCaller_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ConstructorArgumentNull_CaughtByCaller_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ConstructorArgumentNull_PermittedByCaller_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ConstructorArgumentNull_PermittedByCaller_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ConstructorArgumentNull_CaughtInsideConstructor_DoesNotReport()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ConstructorArgumentNull_CaughtInsideConstructor_DoesNotReport)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ConstructorArgumentNull_OtherEscapingException_ReportsExc002()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ConstructorArgumentNull_OtherEscapingException_ReportsExc002)));

            Diagnostic mismatch = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(mismatch.GetMessage(), Does.Contain("System.InvalidOperationException"));
            Diagnostic caller = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(caller.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(diagnostics.Where(item => item.Id == "EXC002").Select(item => item.GetMessage()),
                Has.No.Contains("System.ArgumentNullException"));
        }

        [Test]
        public void MethodArgumentNull_DoesNotApplyThrowsToParent()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(MethodArgumentNull_DoesNotApplyThrowsToParent)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void CallerOfConstructorCaller_DoesNotInheritThrows()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(CallerOfConstructorCaller_DoesNotInheritThrows)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(span(diagnostic), Does.Contain("new Resource"));
        }

        [Test]
        public void NullableConstructorArgumentNull_ReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableConstructorArgumentNull_ReportsExc007)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC007");
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo("ArgumentNullException should be removed from 'Resource constructor' because the constructor is nullable-aware and callers are not expected to pass null"));
            Assert.That(span(diagnostic), Does.Contain("ArgumentNullException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void NullableConstructorArgumentNull_DisabledAnalysis_DoesNotReport()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableConstructorArgumentNull_ReportsExc007)),
                projectSetting: "disable",
                fileSetting: "disable");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void NullableConstructorNullableParameter_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableConstructorNullableParameter_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void NullableConstructorAllowNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableConstructorAllowNull_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void DoesNotThrowConstructorArgumentNull_ReportsExc001()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(DoesNotThrowConstructorArgumentNull_ReportsExc001)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC001"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void ConstructorArgumentNull_CombinesWithDeclaredThrows()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ConstructorArgumentNull_CombinesWithDeclaredThrows)));

            string[] messages = diagnostics
                .Where(item => item.Id == "EXC003")
                .Select(item => item.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("System.ArgumentNullException"));
            Assert.That(messages, Has.Some.Contains("System.IO.IOException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void UnreachableConstructorArgumentNull_DoesNotReport()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(UnreachableConstructorArgumentNull_DoesNotReport)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void LocalFunctionArgumentNull_DoesNotMarkConstructor()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(LocalFunctionArgumentNull_DoesNotMarkConstructor)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ExpressionBodiedConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ExpressionBodiedConstructorArgumentNull_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
        }

        [Test]
        public void NullablePrimaryConstructorArgumentNull_ReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullablePrimaryConstructorArgumentNull_ReportsExc007)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC007");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Resource constructor"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void PrimaryConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(PrimaryConstructorArgumentNull_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void ConstructorInvalidOperation_DoesNotReport()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ConstructorInvalidOperation_DoesNotReport)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void NullableConstructorUnnamedArgumentNull_ReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableConstructorUnnamedArgumentNull_ReportsExc007)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void NullableConstructorMixedParameter_ReportsExc007ForNonNullable()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableConstructorMixedParameter_ReportsExc007ForNonNullable)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void NullableConstructorMixedParameter_KeepsThrowsForNullableParameter()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableConstructorMixedParameter_KeepsThrowsForNullableParameter)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void ChainedConstructor_DoesNotInheritArgumentNullThrows()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ChainedConstructor_DoesNotInheritArgumentNullThrows)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(span(diagnostic), Does.Contain("this"));
        }

        [Test]
        public void ExplicitThrowsArgumentNull_NullableConstructor_StillReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(ExplicitThrowsArgumentNull_NullableConstructor_StillReportsExc007)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void StructConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(StructConstructorArgumentNull_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Resource constructor"));
        }

        [Test]
        public void NullableDoesNotThrowConstructor_ReportsExc001AndExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableDoesNotThrowConstructor_ReportsExc001AndExc007)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC001"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void NullableDisableConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.getDiagnostics(
                ExceptionableAnalyzerTests.getTestCase(nameof(NullableDisableConstructorArgumentNull_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        private static string span(Diagnostic diagnostic) =>
            diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString();
    }
}
