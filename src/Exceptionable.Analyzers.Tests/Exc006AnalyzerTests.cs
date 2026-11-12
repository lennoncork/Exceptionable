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
    public class Exc006AnalyzerTests
    {
        [Test]
        public void DoesNotThrowCallInSingleTryCatch_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DoesNotThrowCallInSingleTryCatch_ReportsExc006)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC006");
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(
                diagnostic.GetMessage(),
                Is.EqualTo("Try/catch is unnecessary because 'Safe' has a [DoesNotThrow] contract"));
            Assert.That(
                diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString(),
                Is.EqualTo("try"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC001"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void ClassDoesNotThrowCallInSingleTryCatch_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ClassDoesNotThrowCallInSingleTryCatch_ReportsExc006)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC006");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Safe' has a [DoesNotThrow] contract"));
        }

        [Test]
        public void InterfaceDoesNotThrowCallInSingleTryCatch_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InterfaceDoesNotThrowCallInSingleTryCatch_ReportsExc006)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC006");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Execute' has a [DoesNotThrow] contract"));
        }

        [Test]
        public void ThrowsCallInSingleTryCatch_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowsCallInSingleTryCatch_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void UnannotatedCallInSingleTryCatch_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UnannotatedCallInSingleTryCatch_DoesNotReportExc006)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void DoesNotThrowCallBesideThrow_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DoesNotThrowCallBesideThrow_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
        }

        [Test]
        public void VirtualDoesNotThrowCallInSingleTryCatch_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(VirtualDoesNotThrowCallInSingleTryCatch_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
        }

        [Test]
        public void DoesNotThrowCallInTryFinally_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DoesNotThrowCallInTryFinally_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
        }

        [Test]
        public void ConditionalDoesNotThrow_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ConditionalDoesNotThrow_ReportsExc006)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC006"), Is.EqualTo(4));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void BaseDoesNotThrowCall_ReportsExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(BaseDoesNotThrowCall_ReportsExc006)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC006");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Safe'"));
        }

        [Test]
        public void ConditionalWithThrow_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ConditionalWithThrow_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC006"));
        }
    }
}