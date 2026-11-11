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
        public void RethrowInThrowsMethod_ReportsCatchType()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(RethrowInThrowsMethod_ReportsCatchType)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'System.InvalidOperationException'"));
        }

        [Test]
        public void BareRethrowInCatchAll_ReportsExceptionType()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(BareRethrowInCatchAll_ReportsExceptionType)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'System.Exception'"));
        }

        [Test]
        public void CallerDeclaringBaseException_AllowsDerivedCalleeContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(CallerDeclaringBaseException_AllowsDerivedCalleeContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void CallerDeclaringDerivedException_DoesNotAllowBaseCalleeContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(CallerDeclaringDerivedException_DoesNotAllowBaseCalleeContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void ThrowInFinally_IsNotCaughtBySameTry()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowInFinally_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void UnreachableThrow_DoesNotReportEscapingException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UnreachableThrow_DoesNotReportEscapingException)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void CallInCatch_IsNotCaughtBySameTry()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(CallInCatch_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void CallInFinally_IsNotCaughtBySameTry()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(CallInFinally_IsNotCaughtBySameTry)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void FilterThatIsNotConstantTrue_DoesNotProveExceptionHandled()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(FilterThatIsNotConstantTrue_DoesNotProveExceptionHandled)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void ShortNameInNestedNamespace_ResolvesExceptionType()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ShortNameInNestedNamespace_ResolvesExceptionType)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void GenericThrows_PermitsDeclaredException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_PermitsDeclaredException)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void GenericThrows_AllowsDerivedException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_AllowsDerivedException)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void GenericThrows_CombinesWithStringThrows()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_CombinesWithStringThrows)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void GenericThrows_ConflictsWithDoesNotThrow()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_ConflictsWithDoesNotThrow)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC005"));
        }

        [Test]
        public void GenericThrows_ClosedGenericException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_ClosedGenericException)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void GenericThrows_IndirectExceptionBase()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_IndirectExceptionBase)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void SourceDefinedGenericThrows_ReportsNonException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(SourceDefinedGenericThrows_ReportsNonException)),
                includeExceptionableAttributes: false);

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC004");
            Assert.That(diagnostic.GetMessage(), Does.Contain("does not derive from System.Exception"));
        }

        [Test]
        public void SourceDefinedGenericThrows_IsRecognized()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(SourceDefinedGenericThrows_IsRecognized)),
                includeExceptionableAttributes: false);

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("IOException"));
        }

        [Test]
        public void UnrelatedGenericThrows_IsNotAContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UnrelatedGenericThrows_IsNotAContract)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void MethodContractOverridesClassContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(MethodContractOverridesClassContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC002"));
        }

        [Test]
        public void InterfaceCall_UsesDeclaredContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InterfaceCall_UsesDeclaredContract)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'System.IO.IOException'"));
        }

        [Test]
        public void InterfaceImplementation_AllowsDeclaredCall()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InterfaceImplementation_AllowsDeclaredCall)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void DoesNotThrowInterface_ReportsEscapingCall()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DoesNotThrowInterface_ReportsEscapingCall)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC009"));
        }

        [Test]
        public void LocalFunctionIterator_DoesNotSuppressOuterThrow()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(LocalFunctionIterator_DoesNotSuppressOuterThrow)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void NonExceptionShortName_ResolvesSystemException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(NonExceptionShortName_ResolvesSystemException)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void NestedExceptionName_ResolvesDottedAndMetadataForms()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(NestedExceptionName_ResolvesDottedAndMetadataForms)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void DerivedClassContract_ReplacesBaseClassContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DerivedClassContract_ReplacesBaseClassContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC001"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC005"));
        }

        [Test]
        public void OverrideContract_ReplacesBaseMethodContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(OverrideContract_ReplacesBaseMethodContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC002"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC005"));
        }

        [Test]
        public void InterfaceImplementation_AllowsDeclaredDerivedException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InterfaceImplementation_AllowsDeclaredDerivedException)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void InterfaceImplementation_ReportsUndeclaredException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InterfaceImplementation_ReportsUndeclaredException)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'System.InvalidOperationException'"));
        }

        [Test]
        public void DoesNotThrowInterfaceContract_ReportsEscapingImplementationException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DoesNotThrowInterfaceContract_ReportsEscapingImplementationException)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC001"), Is.EqualTo(1));
        }

        [Test]
        public void ExplicitInterfaceImplementation_ReportsUndeclaredException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ExplicitInterfaceImplementation_ReportsUndeclaredException)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC002"), Is.EqualTo(1));
        }

        [Test]
        public void InheritedInterfaceContract_IsUsedAtCallSite()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InheritedInterfaceContract_IsUsedAtCallSite)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void MultipleInterfaceContracts_AreAllValidated()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(MultipleInterfaceContracts_AreAllValidated)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'System.ArgumentException'"));
            Assert.That(diagnostics.Count(item => item.Id == "EXC009"), Is.EqualTo(4));
        }

        [Test]
        public void UnrelatedAttributeWithSameShortName_IsNotAnExceptionableContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UnrelatedAttributeWithSameShortName_IsNotAnExceptionableContract)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void SourceDefinedExceptionableAttribute_IsRecognized()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(SourceDefinedExceptionableAttribute_IsRecognized)),
                includeExceptionableAttributes: false);

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void AsyncAndIteratorMethods_AreNotAnalyzedYet()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(AsyncAndIteratorMethods_AreNotAnalyzedYet)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC001"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void SynchronousTaskReturningMethodsAnalyzeImmediateThrowsButCallsRemainDeferred()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase("SynchronousTaskReturningMethodsAnalyzeImmediateThrowsButCallsRemainDeferred.Source1"));

            Diagnostic[] escapes = diagnostics.Where(diagnostic => diagnostic.Id == "EXC001").ToArray();
            Assert.That(escapes, Has.Length.EqualTo(2));
            Assert.That(escapes.Select(diagnostic => diagnostic.GetMessage()),
                Has.Some.Contains("System.InvalidOperationException"));
            Assert.That(escapes.Select(diagnostic => diagnostic.GetMessage()),
                Has.Some.Contains("System.NotSupportedException"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void EditorConfigDisable_OverridesProjectEnable()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(EditorConfigDisable_OverridesProjectEnable)),
                projectSetting: "enable",
                fileSetting: "disable");

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void PerDiagnosticSeverityDoesNotEnableAnalysis()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(
                AnalyzerTestHarness.GetTestCase(nameof(PerDiagnosticSeverityDoesNotEnableAnalysis)),
                projectSetting: null,
                fileSetting: null);

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ThrowNull_ReportsNullReferenceException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowNull_ReportsNullReferenceException)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC001");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'System.NullReferenceException'"));
        }

        [Test]
        public void ThrowDefaultException_ReportsNullReferenceException()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowDefaultException_ReportsNullReferenceException)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC001");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'System.NullReferenceException'"));
        }

        [Test]
        public void LocalFunctionThrow_DoesNotEscapeOuterMethod()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(LocalFunctionThrow_DoesNotEscapeOuterMethod)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void LambdaThrow_DoesNotEscapeOuterMethod()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(LambdaThrow_DoesNotEscapeOuterMethod)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void RecordCopyConstructor_DoesNotAttributeInitializer()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(RecordCopyConstructor_DoesNotAttributeInitializer)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ForeachGetEnumerator_ReportsDeferredReturnType()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ForeachGetEnumerator_ReportsDeferredReturnType)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'GetEnumerator'"));
        }

        [Test]
        public void AccessorAttribute_OverridesPropertyAttribute()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(AccessorAttribute_OverridesPropertyAttribute)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Value setter"));
        }
    }
}
