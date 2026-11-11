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
            string source = AnalyzerTestHarness.GetTestCase("TryGetThrowsName_TrimsValidAttributeNames.Source1");
            var compilation = TestCompilation.Create(source);
            var attribute = compilation.GetTypeByMetadataName("C")!.GetAttributes().Single();

            Assert.That(ThrowsAttributeReader.IsThrows(attribute), Is.True);
            Assert.That(ThrowsAttributeReader.TryGetThrowsName(attribute, out var name), Is.True);
            Assert.That(name, Is.EqualTo("System.IO.IOException"));
        }

        [Test]
        public void TryGetThrowsName_RejectsWhitespaceOnlyValues()
        {
            string source = AnalyzerTestHarness.GetTestCase("TryGetThrowsName_RejectsWhitespaceOnlyValues.Source2");
            var compilation = TestCompilation.Create(source);
            var attribute = compilation.GetTypeByMetadataName("C")!.GetAttributes().Single();

            Assert.That(ThrowsAttributeReader.TryGetThrowsName(attribute, out var name), Is.False);
            Assert.That(name, Is.Empty);
        }

        [Test]
        public void IsDoesNotThrow_RecognizesTheExceptionableAttribute()
        {
            string source = AnalyzerTestHarness.GetTestCase("IsDoesNotThrow_RecognizesTheExceptionableAttribute.Source3");
            var compilation = TestCompilation.Create(source);
            var attribute = compilation.GetTypeByMetadataName("C")!.GetAttributes().Single();

            Assert.That(ThrowsAttributeReader.IsDoesNotThrow(attribute), Is.True);
        }

        [Test]
        public void TryGetThrowsType_ReadsTheExceptionTypeArgument()
        {
            string source = AnalyzerTestHarness.GetTestCase("TryGetThrowsType_ReadsTheExceptionTypeArgument.Source4");
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
            string source = AnalyzerTestHarness.GetTestCase("IsThrows_IgnoresAGenericAttributeInAnotherNamespace.Source5");
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
            string source = AnalyzerTestHarness.GetTestCase("GenericThrowsAttribute_RejectsANonExceptionTypeArgument.Source6");
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