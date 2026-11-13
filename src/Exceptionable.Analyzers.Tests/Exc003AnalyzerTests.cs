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
    public class Exc003AnalyzerTests
    {
        [Test]
        public void ThrowingAnnotatedCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThrowingAnnotatedCall_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void AnnotatedConstructorCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(AnnotatedConstructorCall_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Resource constructor"));
        }

        [Test]
        public void AnnotatedPropertyAccessors_ReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(AnnotatedPropertyAccessors_ReportExc003)));

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
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(CompoundPropertyAssignment_ReportsExc003ForBothAccessors)));

            Assert.That(diagnostics.Count(diagnostic => diagnostic.Id == "EXC003"), Is.EqualTo(2));
        }

        [Test]
        public void AnnotatedEventAccessors_ReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(AnnotatedEventAccessors_ReportExc003)));

            string[] messages = diagnostics
                .Where(diagnostic => diagnostic.Id == "EXC003")
                .Select(diagnostic => diagnostic.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("Changed add accessor"));
            Assert.That(messages, Has.Some.Contains("Changed remove accessor"));
        }

        [Test]
        public void UnreachableCall_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UnreachableCall_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void CaughtCall_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(CaughtCall_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void GenericThrows_ReportsExc003AtCaller()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_ReportsExc003AtCaller)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("IOException"));
        }

        [Test]
        public void GenericThrows_PropertyAccessorReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(GenericThrows_PropertyAccessorReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Value getter"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("IOException"));
        }

        [Test]
        public void ClassContract_ReportsExc003ForInstanceCall()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ClassContract_ReportsExc003ForInstanceCall)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void InterfaceContractNarrowsCaller_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InterfaceContractNarrowsCaller_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC009"));
        }

        [Test]
        public void SealedOverrideCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(SealedOverrideCall_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC002"));
        }

        [Test]
        public void VirtualCall_ReportsStaticContract()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(VirtualCall_ReportsStaticContract)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.IO.IOException"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC002"));
        }

        public void VirtualCall_InferredThrow_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(VirtualCall_InferredThrow_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void BaseVirtualCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(BaseVirtualCall_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'M'"));
            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC001"));
        }

        [Test]
        public void BasePropertyAccess_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(BasePropertyAccess_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void NameOfProperty_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(NameOfProperty_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void DeconstructionAssignment_ReportsSetterExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DeconstructionAssignment_ReportsSetterExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Value setter"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("InvalidOperationException"));
        }

        [Test]
        public void DeconstructMethod_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(DeconstructMethod_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Deconstruct'"));
        }

        [Test]
        public void FieldInitializer_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(FieldInitializer_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void PropertyInitializer_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(PropertyInitializer_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void FieldInitializer_PermittedByConstructor_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(FieldInitializer_PermittedByConstructor_DoesNotReportExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void ThisChainedConstructor_ReportsExc003ForConstructorCall()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ThisChainedConstructor_ReportsExc003ForConstructorCall)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("C constructor"));
            Assert.That(
                diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString(),
                Does.Contain("this(0)"));
        }

        [Test]
        public void UserDefinedRecordCopyConstructor_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UserDefinedRecordCopyConstructor_ReportsExc003)));

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void RedeclaredInterfaceContract_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(RedeclaredInterfaceContract_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'System.IO.IOException'"));
        }

        [Test]
        public void SameShortNameExceptions_ReportSeparateExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(SameShortNameExceptions_ReportSeparateExc003)));

            string[] messages = diagnostics
                .Where(diagnostic => diagnostic.Id == "EXC003")
                .Select(diagnostic => diagnostic.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("A.SharedException"));
            Assert.That(messages, Has.Some.Contains("B.SharedException"));
        }

        [Test]
        public void StaticFieldInitializer_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(StaticFieldInitializer_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void ForeachMoveNext_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ForeachMoveNext_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'MoveNext'"));
            Assert.That(
                diagnostic.Location.SourceTree!.GetText().GetSubText(diagnostic.Location.SourceSpan).ToString(),
                Is.EqualTo("foreach"));
        }

        [Test]
        public void UsingDispose_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UsingDispose_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("'Dispose'"));
        }

        [Test]
        public void UsingStatementDispose_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UsingStatementDispose_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void UsingDispose_CaughtDoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(UsingDispose_CaughtDoesNotReportExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void ExplicitDispose_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ExplicitDispose_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void OperatorCall_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(OperatorCall_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("operator +"));
        }

        [Test]
        public void ConversionOperator_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(ConversionOperator_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("explicit conversion"));
        }

        [Test]
        public void BuiltInArithmetic_DoesNotReportExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(BuiltInArithmetic_DoesNotReportExc003)));

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void PropertyAttribute_ReportsExc003ForAccessors()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(PropertyAttribute_ReportsExc003ForAccessors)));

            string[] messages = diagnostics
                .Where(diagnostic => diagnostic.Id == "EXC003")
                .Select(diagnostic => diagnostic.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("Value getter"));
            Assert.That(messages, Has.Some.Contains("Value setter"));
        }

        [Test]
        public void InterfacePropertyAttribute_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(InterfacePropertyAttribute_ReportsExc003)));

            Assert.That(diagnostics.Select(item => item.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void EventAttribute_ReportsExc003()
        {
            Diagnostic[] diagnostics = AnalyzerTestHarness.GetDiagnostics(AnalyzerTestHarness.GetTestCase(nameof(EventAttribute_ReportsExc003)));

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("Changed add accessor"));
        }
    }
}