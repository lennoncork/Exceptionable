using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis.CSharp;

namespace Exceptionable.Analyzers.Tests
{
    public class ExceptionTypeResolverTests
    {
        private const string source = @"
namespace First
{
    public class SharedException : System.Exception { }
    public class CustomException : System.Exception { }
}
namespace Second
{
    public class SharedException : System.Exception { }
}
namespace Other
{
    public class NotAnException { }
    public class CustomException { }
}
namespace Nested
{
    public class Outer
    {
        public class NestedException : System.Exception { }
    }
}
public class Outer
{
    public class NestedException : System.Exception { }
}";

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
            Assert.That(resolver.Resolve("MissingException").IsAmbiguous, Is.False);
            Assert.That(resolver.ResolveExceptionType("MissingException"), Is.Null);
            Assert.That(resolver.ResolveExceptionType("NestedException"), Is.Null);
            Assert.That(
                resolver.ResolveExceptionType("global::System.IO.IOException")?.ToDisplayString(),
                Is.EqualTo("System.IO.IOException"));
        }
    }
}