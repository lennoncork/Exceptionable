using Exceptionable.Analyzers.Contracts;

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
}";

        [Test]
        public void ResolveExceptionType_ResolvesFullyQualifiedAndUniqueShortNames()
        {
            var compilation = TestCompilation.Create(source);
            var resolver = new ExceptionTypeResolver(compilation);

            Assert.That(resolver.ResolveExceptionType("First.CustomException")?.Name, Is.EqualTo("CustomException"));
            Assert.That(resolver.ResolveExceptionType("CustomException")?.Name, Is.EqualTo("CustomException"));
            Assert.That(resolver.ResolveExceptionType("Exception")?.ToDisplayString(), Is.EqualTo("System.Exception"));
        }

        [Test]
        public void ResolveExceptionType_RejectsAmbiguousAndNonExceptionNames()
        {
            var compilation = TestCompilation.Create(source);
            var resolver = new ExceptionTypeResolver(compilation);

            Assert.That(resolver.ResolveExceptionType("SharedException"), Is.Null);
            Assert.That(resolver.ResolveExceptionType("Other.NotAnException"), Is.Null);
            Assert.That(resolver.ResolveExceptionType("MissingException"), Is.Null);
        }
    }
}