using System.Linq;
using Exceptionable.Analyzers.Analysis;

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
class C
{
    async Task AsyncMethod() { await Task.CompletedTask; }
    IEnumerable<int> IteratorMethod() { yield return 1; }
    void SynchronousMethod() { }
    virtual void VirtualMethod() { }
}";
            var compilation = TestCompilation.Create(source);

            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "AsyncMethod")), Is.False);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "IteratorMethod")), Is.False);
            Assert.That(MethodAnalysisPolicy.IsSupportedMethod(
                TestCompilation.GetMethod(compilation, "C", "SynchronousMethod")), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDispatchUncertain(
                TestCompilation.GetMethod(compilation, "C", "VirtualMethod")), Is.True);
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
    IEnumerable<int> Items() => null;
    int Value() => 0;
}";
            var compilation = TestCompilation.Create(source);
            var methods = new[]
            {
                TestCompilation.GetMethod(compilation, "C", "Work"),
                TestCompilation.GetMethod(compilation, "C", "Items"),
                TestCompilation.GetMethod(compilation, "C", "Value"),
            };

            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[0].ReturnType), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[1].ReturnType), Is.True);
            Assert.That(MethodAnalysisPolicy.IsDeferredReturnType(methods[2].ReturnType), Is.False);
        }
    }
}