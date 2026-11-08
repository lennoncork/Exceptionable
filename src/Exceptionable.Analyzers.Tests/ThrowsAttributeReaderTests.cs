using System.Linq;
using Exceptionable.Analyzers.Contracts;

namespace Exceptionable.Analyzers.Tests
{
    public class ThrowsAttributeReaderTests
    {
        [Test]
        public void TryGetThrowsName_TrimsValidAttributeNames()
        {
            const string source = @"
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
}
[Exceptionable.Throws(""  System.IO.IOException  "")]
class C { }";
            var compilation = TestCompilation.Create(source);
            var attribute = compilation.GetTypeByMetadataName("C")!.GetAttributes().Single();

            Assert.That(ThrowsAttributeReader.IsThrows(attribute), Is.True);
            Assert.That(ThrowsAttributeReader.TryGetThrowsName(attribute, out var name), Is.True);
            Assert.That(name, Is.EqualTo("System.IO.IOException"));
        }

        [Test]
        public void TryGetThrowsName_RejectsWhitespaceOnlyValues()
        {
            const string source = @"
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
}
[Exceptionable.Throws(""  "")]
class C { }";
            var compilation = TestCompilation.Create(source);
            var attribute = compilation.GetTypeByMetadataName("C")!.GetAttributes().Single();

            Assert.That(ThrowsAttributeReader.TryGetThrowsName(attribute, out var name), Is.False);
            Assert.That(name, Is.Empty);
        }

        [Test]
        public void IsDoesNotThrow_RecognizesTheExceptionableAttribute()
        {
            const string source = @"
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class DoesNotThrowAttribute : Attribute { }
}
[Exceptionable.DoesNotThrow]
class C { }";
            var compilation = TestCompilation.Create(source);
            var attribute = compilation.GetTypeByMetadataName("C")!.GetAttributes().Single();

            Assert.That(ThrowsAttributeReader.IsDoesNotThrow(attribute), Is.True);
        }
    }
}