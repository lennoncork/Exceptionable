using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class PolyfillTests
    {
        [Test]
        public void ReadAllText_EnabledByDefault()
        {
            Diagnostic[] diagnostics = getDiagnostics("Polyfill_FileReadAllText");

            Assert.That(diagnostics.Select(item => item.Id), Is.All.EqualTo("EXC003"));
            Assert.That(
                diagnostics.Select(item => item.GetMessage()),
                Has.Some.Contain("'System.IO.IOException'"));
            Assert.That(
                diagnostics.Select(item => item.GetMessage()),
                Has.Some.Contain("'System.UnauthorizedAccessException'"));
            Assert.That(diagnostics, Has.Length.EqualTo(2));
        }

        [Test]
        public void ReadAllText_NamespaceSettingDisablesPolyfill()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_FileReadAllText",
                ("exceptionable_polyfill.System.IO", "disable"));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ReadAllText_ParentNamespaceSettingApplies()
        {
            Diagnostic[] disabled = getDiagnostics(
                "Polyfill_FileReadAllText",
                ("exceptionable_polyfill.System", "disable"));
            Diagnostic[] reenabled = getDiagnostics(
                "Polyfill_FileReadAllText",
                ("exceptionable_polyfill.System", "disable"),
                ("exceptionable_polyfill.System.IO", "enable"));

            Assert.That(disabled, Is.Empty);
            Assert.That(reenabled, Has.Length.EqualTo(2));
        }

        [Test]
        public void ReadAllText_ProjectPropertyAndFileSettingPrecedence()
        {
            Diagnostic[] disabled = getDiagnostics(
                "Polyfill_FileReadAllText",
                ("build_property.ExceptionablePolyfills", "disable"));
            Diagnostic[] overridden = getDiagnostics(
                "Polyfill_FileReadAllText",
                ("build_property.ExceptionablePolyfills", "disable"),
                ("exceptionable_polyfill", "enable"));

            Assert.That(disabled, Is.Empty);
            Assert.That(overridden, Has.Length.EqualTo(2));
        }

        [Test]
        public void ReadAllText_InvalidValueDoesNotOverrideParentSetting()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_FileReadAllText",
                ("exceptionable_polyfill", "disable"),
                ("exceptionable_polyfill.System.IO", "on"));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ReadAllText_AnalysisDisabledSuppressesPolyfill()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase("Polyfill_FileReadAllText"),
                fileSetting: "disable",
                additionalOptions: new Dictionary<string, string> { ["exceptionable_polyfill"] = "enable" });

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ReadAllText_CaughtOrDeclaredDoesNotReport()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_FileReadAllText_HandledOrDeclared");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void SourceMemberWithPolyfilledName_IsNotMatched()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_SourceMemberNotMatched");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void DoesNotThrowCaller_ReportsLinqCollectionAndTaskCalls()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_DoesNotThrowCaller");

            string[] messages = [.. diagnostics.Select(item => item.GetMessage())];
            Assert.That(messages, Has.Length.EqualTo(3));
            Assert.That(messages, Has.Some.Contain("'System.InvalidOperationException'"));
            Assert.That(messages, Has.Some.Contain("'System.Collections.Generic.KeyNotFoundException'"));
            Assert.That(messages, Has.Some.Contain("'System.AggregateException'"));
        }

        [Test]
        public void DoesNotThrowCaller_NamespaceCanBeDisabledIndividually()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_DoesNotThrowCaller",
                ("exceptionable_polyfill.System.Linq", "disable"));

            Assert.That(diagnostics, Has.Length.EqualTo(2));
            Assert.That(diagnostics.Select(item => item.GetMessage()), Has.None.Contain("InvalidOperationException"));
        }

        [Test]
        public void NoThrowCalls_ReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics("Polyfill_NoThrowCalls_ReportExc006");

            Diagnostic[] unnecessary = [.. diagnostics.Where(item => item.Id == "EXC006")];
            Assert.That(unnecessary, Has.Length.EqualTo(9));
            Assert.That(diagnostics.Count(item => item.Id == "EXC009"), Is.EqualTo(1));
            Assert.That(lines(unnecessary), Has.Some.Contain("comparer.Equals"));
            Assert.That(lines(unnecessary), Has.Some.Contain("value.ToString()"));
            Assert.That(lines(unnecessary), Has.None.Contain("token.ToString()"));
        }

        [Test]
        public void NoThrowCalls_GenericNamespaceCanBeDisabled()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_NoThrowCalls_ReportExc006",
                ("exceptionable_polyfill.System.Collections.Generic", "disable"));

            Diagnostic[] unnecessary = [.. diagnostics.Where(item => item.Id == "EXC006")];
            Assert.That(unnecessary, Has.Length.EqualTo(8));
            Assert.That(diagnostics.Count(item => item.Id == "EXC009"), Is.EqualTo(1));
            Assert.That(lines(unnecessary), Has.None.Contain("comparer.Equals"));
        }

        [Test]
        public void NoThrowCalls_SystemNamespaceDisablesAnchor()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                "Polyfill_NoThrowCalls_ReportExc006",
                ("exceptionable_polyfill.System", "disable"));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ThrowingOverloads_DoNotReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics("Polyfill_ThrowingOverloads_DoNotReportExc006");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void NoThrowImplementation_ReportsExc001AndSealedCall()
        {
            Diagnostic[] diagnostics = getDiagnostics("Polyfill_NoThrowImplementation_ReportsExc001");
            Diagnostic[] escapes = [.. diagnostics.Where(item => item.Id == "EXC001")];
            Diagnostic[] calls = [.. diagnostics.Where(item => item.Id == "EXC003")];

            Assert.That(diagnostics, Has.Length.EqualTo(7));
            Assert.That(escapes, Has.Length.EqualTo(3));
            Assert.That(diagnostics.Count(item => item.Id == "EXC009"), Is.EqualTo(3));
            Assert.That(escapes.Select(item => item.GetMessage()), Is.All.Contain("'System.InvalidOperationException'"));
            Assert.That(calls, Has.Length.EqualTo(1));
            Assert.That(calls[0].GetMessage(), Does.Contain("'ToString'"));
            Assert.That(lines(calls), Has.Some.Contain("ToString()"));
            Assert.That(diagnostics.Select(item => item.Id), Has.None.EqualTo("EXC006"));
        }

        [Test]
        public void ExplicitThrows_DoesNotReplaceNoThrowPolyfill()
        {
            Diagnostic[] diagnostics = getDiagnostics("Polyfill_ExplicitThrows_ReplacesNoThrowPolyfill");

            Assert.That(diagnostics.Select(item => item.Id), Is.EquivalentTo(new[] { "EXC001", "EXC009" }));
            Assert.That(diagnostics.Single(item => item.Id == "EXC009").GetMessage(), Does.Contain("ToString").And.Contain("object.ToString"));
        }

        [Test]
        public void CaughtToStringThrow_DoesNotReportExc001()
        {
            Diagnostic[] diagnostics = getDiagnostics("Polyfill_CaughtToStringThrow_DoesNotReportExc001");

            Assert.That(diagnostics, Is.Empty);
        }

        private static string[] lines(Diagnostic[] diagnostics) =>
        [
            .. diagnostics.Select(diagnostic =>
                diagnostic.Location.SourceTree!.GetText().Lines[diagnostic.Location.GetLineSpan().StartLinePosition.Line].ToString()),
        ];

        private static Diagnostic[] getDiagnostics(string testCase, params (string Key, string Value)[] options) =>
            AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(testCase),
                additionalOptions: options.ToDictionary(option => option.Key, option => option.Value));
    }
}