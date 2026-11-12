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
    public class Exc005AnalyzerTests
    {
        [Test]
        public void ConflictingClassAttributes_ReportsExc005()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ConflictingClassAttributes_ReportsExc005)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC005"));
        }
    }
}