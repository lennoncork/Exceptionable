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
            const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class Base
{
    public virtual void SealedOverride() { }
}
class C : Base
{
    async Task AsyncMethod() { await Task.CompletedTask; }
    IEnumerable<int> IteratorMethod() { yield return 1; }
    void SynchronousMethod() { }
    virtual void VirtualMethod() { }
    void CallsLocalIterator()
    {
        System.Collections.Generic.IEnumerable<int> Local() { yield return 1; }
    }
    public sealed override void SealedOverride() { }
}";
            CSharpCompilation compilation = TestCompilation.Create(source);

            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "AsyncMethod")), Is.False);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "IteratorMethod")), Is.False);
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
            const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    Task Work() => Task.CompletedTask;
    ValueTask WorkValue() => default;
    IEnumerable<int> Items() => null;
    IAsyncEnumerable<int> Stream() => null;
    IAsyncEnumerator<int> Move() => null;
    int Value() => 0;
}";
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