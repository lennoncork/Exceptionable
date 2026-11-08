using Exceptionable.Analyzers.Contracts;

namespace Exceptionable.Analyzers.Tests
{
    public class SymbolExtensionsTests
    {
        [Test]
        public void InheritsOrEquals_RecognizesSameAndBaseExceptionTypes()
        {
            const string source = @"
using System;
class CustomException : Exception { }
class DerivedException : CustomException { }
class OtherException : Exception { }";
            var compilation = TestCompilation.Create(source);
            var customException = compilation.GetTypeByMetadataName("CustomException")!;
            var derivedException = compilation.GetTypeByMetadataName("DerivedException")!;
            var otherException = compilation.GetTypeByMetadataName("OtherException")!;

            Assert.That(derivedException.InheritsOrEquals(customException), Is.True);
            Assert.That(customException.InheritsOrEquals(customException), Is.True);
            Assert.That(customException.InheritsOrEquals(otherException), Is.False);
        }

        [Test]
        public void IsSameMethod_RecognizesEquivalentMethodSymbols()
        {
            const string source = @"
class C
{
    void M() { }
}";
            var compilation = TestCompilation.Create(source);
            var first = TestCompilation.GetMethod(compilation, "C", "M");
            var second = TestCompilation.GetMethod(compilation, "C", "M");

            Assert.That(first.IsSameMethod(second), Is.True);
        }
    }
}