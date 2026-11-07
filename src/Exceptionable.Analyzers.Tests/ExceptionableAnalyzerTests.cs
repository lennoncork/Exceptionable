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
    public class ExceptionableAnalyzerTests
    {
        [Test]
        public void DoesNotThrowMethod_ReportsExc001()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(DoesNotThrowMethod_ReportsExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void ThrowsMethod_ReportsExc002_WhenTypeEscapes()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(ThrowsMethod_ReportsExc002_WhenTypeEscapes)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC002"));
        }

        [Test]
        public void ThrowingAnnotatedCall_ReportsExc003()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(ThrowingAnnotatedCall_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void CallerDeclaringBaseException_AllowsDerivedCalleeContract()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(CallerDeclaringBaseException_AllowsDerivedCalleeContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void CallerDeclaringDerivedException_DoesNotAllowBaseCalleeContract()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(CallerDeclaringDerivedException_DoesNotAllowBaseCalleeContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void AnnotatedConstructorCall_ReportsExc003()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(AnnotatedConstructorCall_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void AnnotatedPropertyAccessors_ReportExc003()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(AnnotatedPropertyAccessors_ReportExc003)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC003"), Is.EqualTo(2));
        }

        [Test]
        public void CompoundPropertyAssignment_ReportsExc003ForBothAccessors()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(CompoundPropertyAssignment_ReportsExc003ForBothAccessors)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC003"), Is.EqualTo(2));
        }

        [Test]
        public void AnnotatedEventAccessors_ReportExc003()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(AnnotatedEventAccessors_ReportExc003)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC003"), Is.EqualTo(2));
        }

        [Test]
        public void RethrowInDoesNotThrowMethod_ReportsExc001()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(RethrowInDoesNotThrowMethod_ReportsExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void ThrowInFinally_IsNotCaughtBySameTry()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(ThrowInFinally_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void UnreachableThrow_DoesNotReportEscapingException()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(UnreachableThrow_DoesNotReportEscapingException)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void CallInCatch_IsNotCaughtBySameTry()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(CallInCatch_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void UnreachableCall_DoesNotReportExc003()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(UnreachableCall_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void CallInFinally_IsNotCaughtBySameTry()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(CallInFinally_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void CaughtCall_DoesNotReportExc003()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(CaughtCall_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void FilterThatIsNotConstantTrue_DoesNotProveExceptionHandled()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(FilterThatIsNotConstantTrue_DoesNotProveExceptionHandled)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void ShortNameInNestedNamespace_ResolvesExceptionType()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(ShortNameInNestedNamespace_ResolvesExceptionType)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void AmbiguousShortName_ReportsExc004()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(AmbiguousShortName_ReportsExc004)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC004"));
        }

        [Test]
        public void NonExceptionTypeInThrows_ReportsExc004()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(NonExceptionTypeInThrows_ReportsExc004)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC004"));
        }

        [Test]
        public void ClassLevelInvalidThrows_ReportsExc004()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(ClassLevelInvalidThrows_ReportsExc004)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC004"));
        }

        [Test]
        public void ConflictingClassAttributes_ReportsExc005()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(ConflictingClassAttributes_ReportsExc005)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC005"));
        }

        [Test]
        public void MethodContractOverridesClassContract()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(MethodContractOverridesClassContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC002"));
        }

        [Test]
        public void ClassContractIsInheritedByDerivedType()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(ClassContractIsInheritedByDerivedType)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void UnrelatedAttributeWithSameShortName_IsNotAnExceptionableContract()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(UnrelatedAttributeWithSameShortName_IsNotAnExceptionableContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void SourceDefinedExceptionableAttribute_IsRecognized()
        {
            var diagnostics = getDiagnostics(
                getTestCase(nameof(SourceDefinedExceptionableAttribute_IsRecognized)),
                includeExceptionableAttributes: false);

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void AsyncAndIteratorMethods_AreNotAnalyzedYet()
        {
            var diagnostics = getDiagnostics(getTestCase(nameof(AsyncAndIteratorMethods_AreNotAnalyzedYet)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void EditorConfigDisable_OverridesProjectEnable()
        {
            var diagnostics = getDiagnostics(
                getTestCase(nameof(EditorConfigDisable_OverridesProjectEnable)),
                projectSetting: "enable",
                fileSetting: "disable");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void PerDiagnosticSeverityDoesNotEnableAnalysis()
        {
            var diagnostics = getDiagnostics(
                getTestCase(nameof(PerDiagnosticSeverityDoesNotEnableAnalysis)),
                projectSetting: null,
                fileSetting: null);

            Assert.That(diagnostics, Is.Empty);
        }

        private static Diagnostic[] getDiagnostics(
            string source,
            string? projectSetting = "enable",
            string? fileSetting = "enable",
            bool includeExceptionableAttributes = true)
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))
                .ToList();

            if (includeExceptionableAttributes)
            {
                references.Add(MetadataReference.CreateFromFile(typeof(DoesNotThrowAttribute).Assembly.Location));
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create(
                assemblyName: "ExceptionableTests",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var options = new AnalyzerOptions(
                ImmutableArray<AdditionalText>.Empty,
                new TestAnalyzerConfigOptionsProvider(projectSetting, fileSetting));
            var diagnostics = compilation
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ExceptionableAnalyzer()), options)
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult();

            return diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden).ToArray();
        }

        private static string getTestCase(string testName)
        {
            return File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "TestCases",
                $"{testName}.testcase.cs"));
        }

        private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly AnalyzerConfigOptions treeOptions;

            public TestAnalyzerConfigOptionsProvider(string? projectSetting, string? fileSetting)
            {
                this.GlobalOptions = new TestAnalyzerConfigOptions("build_property.Exceptionable", projectSetting);
                this.treeOptions = new TestAnalyzerConfigOptions("exceptionable_analysis", fileSetting);
            }

            public override AnalyzerConfigOptions GlobalOptions { get; }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.treeOptions;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.treeOptions;
        }

        private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly IReadOnlyDictionary<string, string> values;

            public TestAnalyzerConfigOptions(string key, string? value)
            {
                this.values = value is null
                    ? ImmutableDictionary<string, string>.Empty
                    : ImmutableDictionary<string, string>.Empty.Add(key, value);
            }

            public override bool TryGetValue(string key, out string value)
            {
                return this.values.TryGetValue(key, out value!);
            }
        }
    }
}