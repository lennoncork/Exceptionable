using System;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

        [Test]
        public void TryGetThrowsType_ReadsTheExceptionTypeArgument()
        {
            const string source = @"
using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute<T> : Attribute { }
}
class C
{
    [Exceptionable.Throws<System.IO.IOException>]
    public void M() { }
}";
            CSharpCompilation compilation = TestCompilation.Create(source);
            AttributeData attribute = compilation.GetTypeByMetadataName("C")!
                .GetMembers("M")
                .Single()
                .GetAttributes()
                .Single();

            Assert.That(ThrowsAttributeReader.IsThrows(attribute), Is.True);
            Assert.That(ThrowsAttributeReader.TryGetThrowsType(attribute, out ITypeSymbol? type), Is.True);
            Assert.That(type!.ToDisplayString(), Is.EqualTo("System.IO.IOException"));
            Assert.That(ThrowsAttributeReader.TryGetThrowsName(attribute, out string name), Is.False);
            Assert.That(name, Is.Empty);
        }

        [Test]
        public void IsThrows_IgnoresAGenericAttributeInAnotherNamespace()
        {
            const string source = @"
using System;
namespace Other
{
    [AttributeUsage(AttributeTargets.Method)]
    sealed class ThrowsAttribute<T> : Attribute { }
}
class C
{
    [Other.Throws<System.IO.IOException>]
    public void M() { }
}";
            CSharpCompilation compilation = TestCompilation.Create(source);
            AttributeData attribute = compilation.GetTypeByMetadataName("C")!
                .GetMembers("M")
                .Single()
                .GetAttributes()
                .Single();

            Assert.That(ThrowsAttributeReader.IsThrows(attribute), Is.False);
            Assert.That(ThrowsAttributeReader.TryGetThrowsType(attribute, out ITypeSymbol? type), Is.False);
            Assert.That(type, Is.Null);
        }

        [Test]
        public void GenericThrowsAttribute_IsConstrainedToException()
        {
            Type parameter = typeof(global::Exceptionable.ThrowsAttribute<>).GetGenericArguments()[0];

            Assert.That(parameter.GetGenericParameterConstraints(), Is.EqualTo(new[] { typeof(Exception) }));
        }

        [Test]
        public void GenericThrowsAttribute_RejectsANonExceptionTypeArgument()
        {
            const string source = @"
using Exceptionable;
class C
{
    [Throws<string>]
    public void M() { }
}";
            MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(System.IO.Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))
                .Append(MetadataReference.CreateFromFile(typeof(global::Exceptionable.ThrowsAttribute).Assembly.Location))
                .ToArray();
            CSharpCompilation compilation = CSharpCompilation.Create(
                "GenericThrowsConstraint",
                [CSharpSyntaxTree.ParseText(source)],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            Assert.That(
                compilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
                Is.True);
        }
    }
}