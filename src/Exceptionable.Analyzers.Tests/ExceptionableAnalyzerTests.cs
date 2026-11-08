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
        public void DoesNotThrowCallInSingleTryCatch_ReportsExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DoesNotThrowCallInSingleTryCatch_ReportsExc006)));

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
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ClassDoesNotThrowCallInSingleTryCatch_ReportsExc006)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC006");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Safe' has a [DoesNotThrow] contract"));
        }

        [Test]
        public void InterfaceDoesNotThrowCallInSingleTryCatch_ReportsExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InterfaceDoesNotThrowCallInSingleTryCatch_ReportsExc006)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC006");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Execute' has a [DoesNotThrow] contract"));
        }

        [Test]
        public void ThrowsCallInSingleTryCatch_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowsCallInSingleTryCatch_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void UnannotatedCallInSingleTryCatch_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UnannotatedCallInSingleTryCatch_DoesNotReportExc006)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void DoesNotThrowCallBesideThrow_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DoesNotThrowCallBesideThrow_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
        }

        [Test]
        public void VirtualDoesNotThrowCallInSingleTryCatch_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(VirtualDoesNotThrowCallInSingleTryCatch_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
        }

        [Test]
        public void DoesNotThrowCallInTryFinally_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DoesNotThrowCallInTryFinally_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC006"));
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

            string[] messages = diagnostics
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

            string[] messages = diagnostics
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

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC004");
            Assert.That(diagnostic.GetMessage(), Does.Contain("is ambiguous"));
        }

        [Test]
        public void NonExceptionTypeInThrows_ReportsExc004()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(NonExceptionTypeInThrows_ReportsExc004)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC004");
            Assert.That(diagnostic.GetMessage(), Does.Contain("does not derive from System.Exception"));
        }

        [Test]
        public void GenericThrows_PermitsDeclaredException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_PermitsDeclaredException)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void GenericThrows_ReportsExc002WhenTypeEscapes()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_ReportsExc002WhenTypeEscapes)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC002");
            Assert.That(diagnostic.GetMessage(), Does.Contain("InvalidOperationException"));
        }

        [Test]
        public void GenericThrows_ReportsExc003AtCaller()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_ReportsExc003AtCaller)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("IOException"));
        }

        [Test]
        public void GenericThrows_AllowsDerivedException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_AllowsDerivedException)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void GenericThrows_CombinesWithStringThrows()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_CombinesWithStringThrows)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC004"));
        }

        [Test]
        public void GenericThrows_ConflictsWithDoesNotThrow()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_ConflictsWithDoesNotThrow)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC005"));
        }

        [Test]
        public void GenericThrows_ClosedGenericException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_ClosedGenericException)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void GenericThrows_IndirectExceptionBase()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_IndirectExceptionBase)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void GenericThrows_PropertyAccessorReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(GenericThrows_PropertyAccessorReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Value getter"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("IOException"));
        }

        [Test]
        public void SourceDefinedGenericThrows_ReportsNonException()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                getTestCase(nameof(SourceDefinedGenericThrows_ReportsNonException)),
                includeExceptionableAttributes: false);

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC004");
            Assert.That(diagnostic.GetMessage(), Does.Contain("does not derive from System.Exception"));
        }

        [Test]
        public void SourceDefinedGenericThrows_IsRecognized()
        {
            Diagnostic[] diagnostics = getDiagnostics(
                getTestCase(nameof(SourceDefinedGenericThrows_IsRecognized)),
                includeExceptionableAttributes: false);

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("IOException"));
        }

        [Test]
        public void UnrelatedGenericThrows_IsNotAContract()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UnrelatedGenericThrows_IsNotAContract)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC004"));
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

        [Test]
        public void BaseVirtualCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(BaseVirtualCall_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'M'"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void BasePropertyAccess_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(BasePropertyAccess_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void NameOfProperty_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(NameOfProperty_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void ThrowNull_ReportsNullReferenceException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowNull_ReportsNullReferenceException)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC001");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'NullReferenceException'"));
        }

        [Test]
        public void ThrowDefaultException_ReportsNullReferenceException()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowDefaultException_ReportsNullReferenceException)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC001");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Exception type 'NullReferenceException'"));
        }

        [Test]
        public void ThrowNullCaught_DoesNotReportExc001()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThrowNullCaught_DoesNotReportExc001)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void LocalFunctionThrow_DoesNotEscapeOuterMethod()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(LocalFunctionThrow_DoesNotEscapeOuterMethod)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void LambdaThrow_DoesNotEscapeOuterMethod()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(LambdaThrow_DoesNotEscapeOuterMethod)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void DeconstructionAssignment_ReportsSetterExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DeconstructionAssignment_ReportsSetterExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Value setter"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("InvalidOperationException"));
        }

        [Test]
        public void DeconstructMethod_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(DeconstructMethod_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Deconstruct'"));
        }

        [Test]
        public void FieldInitializer_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(FieldInitializer_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void PropertyInitializer_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(PropertyInitializer_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void FieldInitializer_PermittedByConstructor_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(FieldInitializer_PermittedByConstructor_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void ThisChainedConstructor_ReportsExc003ForConstructorCall()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ThisChainedConstructor_ReportsExc003ForConstructorCall)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("C constructor"));
            Assert.That(
                diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString(),
                Does.Contain("this(0)"));
        }

        [Test]
        public void RecordCopyConstructor_DoesNotAttributeInitializer()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(RecordCopyConstructor_DoesNotAttributeInitializer)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void StaticFieldInitializer_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(StaticFieldInitializer_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void ForeachMoveNext_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ForeachMoveNext_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'MoveNext'"));
            Assert.That(
                diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString(),
                Is.EqualTo("foreach"));
        }

        [Test]
        public void ForeachGetEnumerator_ReportsDeferredReturnType()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ForeachGetEnumerator_ReportsDeferredReturnType)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'GetEnumerator'"));
        }

        [Test]
        public void UsingDispose_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UsingDispose_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Dispose'"));
        }

        [Test]
        public void UsingStatementDispose_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UsingStatementDispose_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void UsingDispose_CaughtDoesNotReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(UsingDispose_CaughtDoesNotReportExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void ExplicitDispose_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ExplicitDispose_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void OperatorCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(OperatorCall_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("operator +"));
        }

        [Test]
        public void OperatorBody_ReportsExc001()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(OperatorBody_ReportsExc001)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC001"));
        }

        [Test]
        public void ConversionOperator_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ConversionOperator_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("explicit conversion"));
        }

        [Test]
        public void BuiltInArithmetic_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(BuiltInArithmetic_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void PropertyAttribute_ReportsExc003ForAccessors()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(PropertyAttribute_ReportsExc003ForAccessors)));

            string[] messages = diagnostics
                .Where(diagnostic => diagnostic.Id == "EXC003")
                .Select(diagnostic => diagnostic.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("Value getter"));
            Assert.That(messages, Has.Some.Contains("Value setter"));
        }

        [Test]
        public void AccessorAttribute_OverridesPropertyAttribute()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(AccessorAttribute_OverridesPropertyAttribute)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Value setter"));
        }

        [Test]
        public void InterfacePropertyAttribute_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(InterfacePropertyAttribute_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void EventAttribute_ReportsExc003()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(EventAttribute_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Changed add accessor"));
        }

        [Test]
        public void ConditionalDoesNotThrow_ReportsExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ConditionalDoesNotThrow_ReportsExc006)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC006"), Is.EqualTo(4));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void BaseDoesNotThrowCall_ReportsExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(BaseDoesNotThrowCall_ReportsExc006)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC006");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Safe'"));
        }

        [Test]
        public void ConditionalWithThrow_DoesNotReportExc006()
        {
            Diagnostic[] diagnostics = getDiagnostics(getTestCase(nameof(ConditionalWithThrow_DoesNotReportExc006)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC006"));
        }

        private static Diagnostic[] getDiagnostics(
            string source,
            string? projectSetting = "enable",
            string? fileSetting = "enable",
            bool includeExceptionableAttributes = true)
        {
            List<PortableExecutableReference> references = [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))];

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

            AnalyzerOptions options = new(
                [],
                new TestAnalyzerConfigOptionsProvider(projectSetting, fileSetting));
            string[] compilerErrors = compilation.GetDiagnostics()
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