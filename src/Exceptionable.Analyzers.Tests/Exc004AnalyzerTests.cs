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
    public class Exc004AnalyzerTests
    {
        [Test]
        public void AmbiguousShortName_ReportsExc004()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(AmbiguousShortName_ReportsExc004)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC004");
            Assert.That(diagnostic.GetMessage(), Does.Contain("is ambiguous"));
        }

        [Test]
        public void NonExceptionTypeInThrows_ReportsExc004()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(NonExceptionTypeInThrows_ReportsExc004)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC004");
            Assert.That(diagnostic.GetMessage(), Does.Contain("does not derive from System.Exception"));
        }

        [Test]
        public void ClassLevelInvalidThrows_ReportsExc004()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ClassLevelInvalidThrows_ReportsExc004)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC004"));
        }
    }
}
