using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis.CSharp;

namespace Exceptionable.Analyzers.Tests
{
    public class ExceptionTypeResolverTests
    {
        private static readonly string source = AnalyzerTestHarness.GetTestCase("ExceptionTypeResolverTests.SharedTypes.Source1");

        [Test]
        public void ResolveExceptionType_ResolvesFullyQualifiedAndUniqueShortNames()
        {
            CSharpCompilation compilation = TestCompilation.Create(source);
            ExceptionTypeResolver resolver = new(compilation);

            Assert.That(resolver.ResolveExceptionType("First.CustomException")?.Name, Is.EqualTo("CustomException"));
            Assert.That(resolver.ResolveExceptionType("CustomException")?.ToDisplayString(), Is.EqualTo("First.CustomException"));
            Assert.That(resolver.ResolveExceptionType("Exception")?.ToDisplayString(), Is.EqualTo("System.Exception"));
            Assert.That(
                resolver.ResolveExceptionType("Outer.NestedException")?.ToDisplayString(),
                Is.EqualTo("Outer.NestedException"));
            Assert.That(
                resolver.ResolveExceptionType("Outer+NestedException")?.ToDisplayString(),
                Is.EqualTo("Outer.NestedException"));
            Assert.That(
                resolver.ResolveExceptionType("Nested.Outer.NestedException")?.ToDisplayString(),
                Is.EqualTo("Nested.Outer.NestedException"));
            Assert.That(
                resolver.ResolveExceptionType("Nested.Outer+NestedException")?.ToDisplayString(),
                Is.EqualTo("Nested.Outer.NestedException"));
        }

        [Test]
        public void ResolveExceptionType_RejectsAmbiguousAndNonExceptionNames()
        {
            CSharpCompilation compilation = TestCompilation.Create(source);
            ExceptionTypeResolver resolver = new(compilation);

            Assert.That(resolver.ResolveExceptionType("SharedException"), Is.Null);
            Assert.That(resolver.Resolve("SharedException").IsAmbiguous, Is.True);
            Assert.That(resolver.ResolveExceptionType("Other.NotAnException"), Is.Null);
            Assert.That(resolver.Resolve("Other.NotAnException").IsNotAnException, Is.True);
            Assert.That(resolver.Resolve("System.String").IsNotAnException, Is.True);
            Assert.That(resolver.Resolve("String").IsNotAnException, Is.True);
            Assert.That(resolver.Resolve("MissingException").IsNotAnException, Is.False);
            Assert.That(resolver.Resolve("MissingException").IsAmbiguous, Is.False);
            Assert.That(resolver.ResolveExceptionType("MissingException"), Is.Null);
            Assert.That(resolver.ResolveExceptionType("NestedException"), Is.Null);
            Assert.That(
                resolver.ResolveExceptionType("global::System.IO.IOException")?.ToDisplayString(),
                Is.EqualTo("System.IO.IOException"));
            Assert.That(
                resolver.ResolveExceptionType("N.Outer.NestedException")?.ToDisplayString(),
                Is.EqualTo("N.Outer.NestedException"));
            Assert.That(
                resolver.ResolveExceptionType("N.Outer+NestedException")?.ToDisplayString(),
                Is.EqualTo("N.Outer.NestedException"));
            Assert.That(resolver.Resolve("Ambiguous.Outer.NestedException").IsAmbiguous, Is.True);
            Assert.That(resolver.ResolveExceptionType("Ambiguous.Outer.NestedException"), Is.Null);
        }
    }
}