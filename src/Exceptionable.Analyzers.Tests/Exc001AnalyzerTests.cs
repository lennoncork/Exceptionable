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
    public class Exc001AnalyzerTests
    {
        [Test]
        public void DoesNotThrowMethod_ReportsExc001()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DoesNotThrowMethod_ReportsExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void CatchAllHandler_DoesNotReportExc001()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(CatchAllHandler_DoesNotReportExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void RethrowInDoesNotThrowMethod_ReportsExc001()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(RethrowInDoesNotThrowMethod_ReportsExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void ThrowNullCaught_DoesNotReportExc001()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowNullCaught_DoesNotReportExc001)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void OperatorBody_ReportsExc001()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(OperatorBody_ReportsExc001)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC001"));
        }
    }
}