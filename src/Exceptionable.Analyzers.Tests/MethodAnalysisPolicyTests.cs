using System.Linq;
using Exceptionable.Analyzers.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Exceptionable.Analyzers.Tests
{
    public class MethodAnalysisPolicyTests
    {
        [Test]
        public void IsSupportedMethod_ExcludesAsyncAndIteratorMethods()
        {
            string source = AnalyzerTestHarness.GetTestCase("IsSupportedMethod_ExcludesAsyncAndIteratorMethods.Source1");
            CSharpCompilation compilation = TestCompilation.Create(source);

            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "AsyncMethod")), Is.False);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "AsyncValueTask")), Is.False);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "IteratorMethod")), Is.False);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "AsyncIteratorMethod")), Is.False);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "SynchronousMethod")), Is.True);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "CallsLocalIterator")), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDispatchUncertain(
                TestCompilation.GetMethod(compilation, "C", "VirtualMethod")), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDispatchUncertain(
                TestCompilation.GetMethod(compilation, "C", "SealedOverride")), Is.False);
        }

        [Test]
        public void IsDeferredReturnType_RecognizesStandardDeferredTypesOnly()
        {
            string source = AnalyzerTestHarness.GetTestCase("IsDeferredReturnType_RecognizesStandardDeferredTypesOnly.Source2");
            CSharpCompilation compilation = TestCompilation.Create(source);
            IMethodSymbol[] methods =
            [
                TestCompilation.GetMethod(compilation, "C", "Work"),
                TestCompilation.GetMethod(compilation, "C", "WorkValue"),
                TestCompilation.GetMethod(compilation, "C", "Items"),
                TestCompilation.GetMethod(compilation, "C", "Stream"),
                TestCompilation.GetMethod(compilation, "C", "Move"),
                TestCompilation.GetMethod(compilation, "C", "Value"),
            ];

            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[0].ReturnType), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[1].ReturnType), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[2].ReturnType), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[3].ReturnType), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[4].ReturnType), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[5].ReturnType), Is.False);
        }
    }
}