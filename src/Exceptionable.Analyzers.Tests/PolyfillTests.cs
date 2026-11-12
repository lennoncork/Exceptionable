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

        private static Diagnostic[] getDiagnostics(string testCase, params (string Key, string Value)[] options) =>
            AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(testCase),
                additionalOptions: options.ToDictionary(option => option.Key, option => option.Value));
    }
}