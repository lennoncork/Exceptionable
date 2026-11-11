using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Tests
{
    public class SymbolExtensionsTests
    {
        [Test]
        public void InheritsOrEquals_RecognizesSameAndBaseExceptionTypes()
        {
            string source = AnalyzerTestHarness.GetTestCase("InheritsOrEquals_RecognizesSameAndBaseExceptionTypes.Source1");
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
            string source = AnalyzerTestHarness.GetTestCase("IsSameMethod_RecognizesEquivalentMethodSymbols.Source2");
            var compilation = TestCompilation.Create(source);
            var first = TestCompilation.GetMethod(compilation, "C", "M");
            var second = TestCompilation.GetMethod(compilation, "C", "M");

            Assert.That(first.IsSameMethod(second), Is.True);
        }

        [Test]
        public void IsSameMethod_DistinguishesClosedGenericConstructions()
        {
            string source = AnalyzerTestHarness.GetTestCase("IsSameMethod_DistinguishesClosedGenericConstructions.Source3");
            var compilation = TestCompilation.Create(source);
            var definition = TestCompilation.GetMethod(compilation, "C`1", "M");
            var intMethod = compilation.GetTypeByMetadataName("C`1")!.Construct(
                compilation.GetSpecialType(SpecialType.System_Int32))
                .GetMembers("M").OfType<IMethodSymbol>().Single();
            var stringMethod = compilation.GetTypeByMetadataName("C`1")!.Construct(
                compilation.GetSpecialType(SpecialType.System_String))
                .GetMembers("M").OfType<IMethodSymbol>().Single();

            Assert.That(definition.IsSameMethod(intMethod), Is.True);
            Assert.That(intMethod.IsSameMethod(stringMethod), Is.False);
        }
    }
}