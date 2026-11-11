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
    public class Exc002AnalyzerTests
    {
        [Test]
        public void ThrowsMethod_ReportsExc002_WhenTypeEscapes()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowsMethod_ReportsExc002_WhenTypeEscapes)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'System.InvalidOperationException'"));
        }

        [Test]
        public void ThrowsDeclaredException_DoesNotReportExc002()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowsDeclaredException_DoesNotReportExc002)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void ThrowsDeclaredExceptionWithExceptionInner_DoesNotReportExc002()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowsDeclaredExceptionWithExceptionInner_DoesNotReportExc002)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void ThrowsDerivedException_DoesNotReportExc002WhenBaseIsDeclared()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowsDerivedException_DoesNotReportExc002WhenBaseIsDeclared)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void GenericThrows_ReportsExc002WhenTypeEscapes()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_ReportsExc002WhenTypeEscapes)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("InvalidOperationException"));
        }
    }
}
