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
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DoesNotThrowMethod_ReportsExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void ThrowsMethod_ReportsExc002_WhenTypeEscapes()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowsMethod_ReportsExc002_WhenTypeEscapes)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'InvalidOperationException'"));
        }

        [Test]
        public void ThrowsDeclaredException_DoesNotReportExc002()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowsDeclaredException_DoesNotReportExc002)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void ThrowsDeclaredExceptionWithExceptionInner_DoesNotReportExc002()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowsDeclaredExceptionWithExceptionInner_DoesNotReportExc002)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void ThrowsDerivedException_DoesNotReportExc002WhenBaseIsDeclared()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowsDerivedException_DoesNotReportExc002WhenBaseIsDeclared)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void RethrowInThrowsMethod_ReportsCatchType()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(RethrowInThrowsMethod_ReportsCatchType)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'InvalidOperationException'"));
        }

        [Test]
        public void BareRethrowInCatchAll_ReportsExceptionType()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(BareRethrowInCatchAll_ReportsExceptionType)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'Exception'"));
        }

        [Test]
        public void CatchAllHandler_DoesNotReportExc001()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(CatchAllHandler_DoesNotReportExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void ThrowingAnnotatedCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowingAnnotatedCall_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void CallerDeclaringBaseException_AllowsDerivedCalleeContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(CallerDeclaringBaseException_AllowsDerivedCalleeContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void CallerDeclaringDerivedException_DoesNotAllowBaseCalleeContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(CallerDeclaringDerivedException_DoesNotAllowBaseCalleeContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void AnnotatedConstructorCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(AnnotatedConstructorCall_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Resource constructor"));
        }

        [Test]
        public void AnnotatedPropertyAccessors_ReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(AnnotatedPropertyAccessors_ReportExc003)));

            var messages = diagnostics
                .Where(diagnostic => diagnostic.Id == "EXC003")
                .Select(diagnostic => diagnostic.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("Value getter"));
            Assert.That(messages, Has.Some.Contains("Value setter"));
        }

        [Test]
        public void CompoundPropertyAssignment_ReportsExc003ForBothAccessors()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(CompoundPropertyAssignment_ReportsExc003ForBothAccessors)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC003"), Is.EqualTo(2));
        }

        [Test]
        public void AnnotatedEventAccessors_ReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(AnnotatedEventAccessors_ReportExc003)));

            var messages = diagnostics
                .Where(diagnostic => diagnostic.Id == "EXC003")
                .Select(diagnostic => diagnostic.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("Changed add accessor"));
            Assert.That(messages, Has.Some.Contains("Changed remove accessor"));
        }

        [Test]
        public void RethrowInDoesNotThrowMethod_ReportsExc001()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(RethrowInDoesNotThrowMethod_ReportsExc001)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void ThrowInFinally_IsNotCaughtBySameTry()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowInFinally_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void UnreachableThrow_DoesNotReportEscapingException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UnreachableThrow_DoesNotReportEscapingException)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void CallInCatch_IsNotCaughtBySameTry()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(CallInCatch_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void UnreachableCall_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UnreachableCall_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void CallInFinally_IsNotCaughtBySameTry()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(CallInFinally_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void CaughtCall_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(CaughtCall_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void FilterThatIsNotConstantTrue_DoesNotProveExceptionHandled()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(FilterThatIsNotConstantTrue_DoesNotProveExceptionHandled)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void ShortNameInNestedNamespace_ResolvesExceptionType()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ShortNameInNestedNamespace_ResolvesExceptionType)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void AmbiguousShortName_ReportsExc004()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(AmbiguousShortName_ReportsExc004)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC004"));
        }

        [Test]
        public void NonExceptionTypeInThrows_ReportsExc004()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(NonExceptionTypeInThrows_ReportsExc004)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC004"));
        }

        [Test]
        public void ClassLevelInvalidThrows_ReportsExc004()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ClassLevelInvalidThrows_ReportsExc004)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC004"));
        }

        [Test]
        public void ConflictingClassAttributes_ReportsExc005()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ConflictingClassAttributes_ReportsExc005)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC005"));
        }

        [Test]
        public void MethodContractOverridesClassContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(MethodContractOverridesClassContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC002"));
        }

        [Test]
        public void ClassContract_ReportsExc003ForInstanceCall()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ClassContract_ReportsExc003ForInstanceCall)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void InterfaceCall_UsesDeclaredContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InterfaceCall_UsesDeclaredContract)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'IOException'"));
        }

        [Test]
        public void InterfaceImplementation_AllowsDeclaredCall()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InterfaceImplementation_AllowsDeclaredCall)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void DoesNotThrowInterface_ReportsEscapingCall()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DoesNotThrowInterface_ReportsEscapingCall)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void InterfaceContractNarrowsCaller_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InterfaceContractNarrowsCaller_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void LocalFunctionIterator_DoesNotSuppressOuterThrow()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(LocalFunctionIterator_DoesNotSuppressOuterThrow)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void NonExceptionShortName_ResolvesSystemException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(NonExceptionShortName_ResolvesSystemException)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void NestedExceptionName_ResolvesDottedAndMetadataForms()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(NestedExceptionName_ResolvesDottedAndMetadataForms)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void SealedOverrideCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(SealedOverrideCall_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void VirtualCall_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(VirtualCall_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void DerivedClassContract_ReplacesBaseClassContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DerivedClassContract_ReplacesBaseClassContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC005"));
        }

        [Test]
        public void OverrideContract_ReplacesBaseMethodContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(OverrideContract_ReplacesBaseMethodContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC002"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC005"));
        }

        [Test]
        public void InterfaceImplementation_AllowsDeclaredDerivedException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InterfaceImplementation_AllowsDeclaredDerivedException)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void InterfaceImplementation_ReportsUndeclaredException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InterfaceImplementation_ReportsUndeclaredException)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'InvalidOperationException'"));
        }

        [Test]
        public void DoesNotThrowInterfaceContract_ReportsEscapingImplementationException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DoesNotThrowInterfaceContract_ReportsEscapingImplementationException)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC001"), Is.EqualTo(1));
        }

        [Test]
        public void ExplicitInterfaceImplementation_ReportsUndeclaredException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ExplicitInterfaceImplementation_ReportsUndeclaredException)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC002"), Is.EqualTo(1));
        }

        [Test]
        public void InheritedInterfaceContract_IsUsedAtCallSite()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InheritedInterfaceContract_IsUsedAtCallSite)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void MultipleInterfaceContracts_AreAllValidated()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(MultipleInterfaceContracts_AreAllValidated)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'ArgumentException'"));
        }

        [Test]
        public void UnrelatedAttributeWithSameShortName_IsNotAnExceptionableContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UnrelatedAttributeWithSameShortName_IsNotAnExceptionableContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void SourceDefinedExceptionableAttribute_IsRecognized()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                getTestCase(nameof(SourceDefinedExceptionableAttribute_IsRecognized)),
                includeExceptionableAttributes: false);

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void AsyncAndIteratorMethods_AreNotAnalyzedYet()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(AsyncAndIteratorMethods_AreNotAnalyzedYet)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void EditorConfigDisable_OverridesProjectEnable()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                getTestCase(nameof(EditorConfigDisable_OverridesProjectEnable)),
                projectSetting: "enable",
                fileSetting: "disable");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void PerDiagnosticSeverityDoesNotEnableAnalysis()
        {
            Diagnostic[] diagnostics = getDiagnostics(
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
            List<PortableExecutableReference> references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))
                .ToList();

            if (includeExceptionableAttributes)
            {
                references.Add(MetadataReference.CreateFromFile(typeof(DoesNotThrowAttribute).Assembly.Location));
            }

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "ExceptionableTests",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            AnalyzerOptions options = new AnalyzerOptions(
                [],
                new TestAnalyzerConfigOptionsProvider(projectSetting, fileSetting));
            var compilerErrors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString())
                .ToArray();
            Assert.That(compilerErrors, Is.Empty, "Analyzer samples must compile.");

            ImmutableArray<Diagnostic> diagnostics = compilation
                .WithAnalyzers([new ExceptionableAnalyzer()], options)
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult();

            return [.. diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden)];
        }

        private static string getTestCase(string testName)
        {
            return File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "TestCases",
                $"{testName}.testcase.cs"));
        }

        private sealed class TestAnalyzerConfigOptionsProvider(string? projectSetting, string? fileSetting) : AnalyzerConfigOptionsProvider
        {
            private readonly AnalyzerConfigOptions treeOptions = new TestAnalyzerConfigOptions("exceptionable_analysis", fileSetting);

            public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions("build_property.Exceptionable", projectSetting);

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.treeOptions;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.treeOptions;
        }

        private sealed class TestAnalyzerConfigOptions(string key, string? value) : AnalyzerConfigOptions
        {
            private readonly IReadOnlyDictionary<string, string> values = value is null
                    ? []
                    : ImmutableDictionary<string, string>.Empty.Add(key, value);

            public override bool TryGetValue(string key, out string value)
            {
                return this.values.TryGetValue(key, out value!);
            }
        }
    }
}