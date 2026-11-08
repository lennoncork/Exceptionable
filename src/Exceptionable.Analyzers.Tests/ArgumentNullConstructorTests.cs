using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class ArgumentNullConstructorTests
    {
        [Test]
        public void ConstructorArgumentNull_ReportsExc003AtCaller()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ConstructorArgumentNull_ReportsExc003AtCaller)));

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
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ConstructorArgumentNull_CaughtByCaller_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ConstructorArgumentNull_PermittedByCaller_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ConstructorArgumentNull_PermittedByCaller_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ConstructorArgumentNull_CaughtInsideConstructor_DoesNotReport()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ConstructorArgumentNull_CaughtInsideConstructor_DoesNotReport)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void UnannotatedConstructor_InfersEveryEscapingThrow()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(UnannotatedConstructor_InfersEveryEscapingThrow)));

            string[] messages = diagnostics
                .Where(item => item.Id == "EXC003")
                .Select(item => item.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("System.ArgumentNullException"));
            Assert.That(messages, Has.Some.Contains("System.InvalidOperationException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void MethodArgumentNull_DoesNotApplyThrowsToParent()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(MethodArgumentNull_DoesNotApplyThrowsToParent)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(span(diagnostic), Does.Contain("Child"));
        }

        [Test]
        public void CallerOfConstructorCaller_DoesNotInheritThrows()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(CallerOfConstructorCaller_DoesNotInheritThrows)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(span(diagnostic), Does.Contain("new Resource"));
        }

        [Test]
        public void NullableConstructorArgumentNull_ReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableConstructorArgumentNull_ReportsExc007)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC007");
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo("ArgumentNullException guard in 'Resource constructor' may be redundant with its nullable parameter contract; nullable annotations do not enforce this at runtime"));
            Assert.That(span(diagnostic), Does.Contain("ArgumentNullException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void NullableConstructorArgumentNull_DisabledAnalysis_DoesNotReport()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableConstructorArgumentNull_ReportsExc007)),
                projectSetting: "disable",
                fileSetting: "disable");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void NullableConstructorNullableParameter_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableConstructorNullableParameter_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void NullableConstructorAllowNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableConstructorAllowNull_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void DoesNotThrowConstructorArgumentNull_ReportsExc001()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(DoesNotThrowConstructorArgumentNull_ReportsExc001)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC001"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void ExplicitThrows_OverridesInferredArgumentNull()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ExplicitThrows_OverridesInferredArgumentNull)));

            Diagnostic mismatch = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(mismatch.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Diagnostic caller = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(caller.GetMessage(), Does.Contain("System.IO.IOException"));
            Assert.That(caller.GetMessage(), Does.Not.Contain("System.ArgumentNullException"));
        }

        [Test]
        public void UnreachableConstructorArgumentNull_DoesNotReport()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(UnreachableConstructorArgumentNull_DoesNotReport)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void LocalFunctionArgumentNull_DoesNotMarkConstructor()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(LocalFunctionArgumentNull_DoesNotMarkConstructor)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ExpressionBodiedConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ExpressionBodiedConstructorArgumentNull_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
        }

        [Test]
        public void NullablePrimaryConstructorArgumentNull_ReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullablePrimaryConstructorArgumentNull_ReportsExc007)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC007");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Resource constructor"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void PrimaryConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(PrimaryConstructorArgumentNull_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void ConstructorInvalidOperation_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ConstructorInvalidOperation_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.InvalidOperationException"));
        }

        [Test]
        public void NullableConstructorUnnamedArgumentNull_ReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableConstructorUnnamedArgumentNull_ReportsExc007)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void NullableConstructorMixedParameter_ReportsExc007ForNonNullable()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableConstructorMixedParameter_ReportsExc007ForNonNullable)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void NullableConstructorMixedParameter_KeepsThrowsForNullableParameter()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableConstructorMixedParameter_KeepsThrowsForNullableParameter)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        [Test]
        public void ChainedConstructor_DoesNotInheritArgumentNullThrows()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ChainedConstructor_DoesNotInheritArgumentNullThrows)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.ArgumentNullException"));
            Assert.That(span(diagnostic), Does.Contain("this"));
        }

        [Test]
        public void ExplicitThrowsArgumentNull_NullableConstructor_StillReportsExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(ExplicitThrowsArgumentNull_NullableConstructor_StillReportsExc007)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void StructConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(StructConstructorArgumentNull_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Resource constructor"));
        }

        [Test]
        public void NullableDoesNotThrowConstructor_ReportsExc001AndExc007()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableDoesNotThrowConstructor_ReportsExc001AndExc007)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC001"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC007"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void NullableDisableConstructorArgumentNull_ReportsExc003()
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase(nameof(NullableDisableConstructorArgumentNull_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC007"));
        }

        private static string span(Diagnostic diagnostic) =>
            diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString();
    }
}