using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Exceptionable.Analyzers.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Exceptionable.Analyzers.Tests
{
    public class InferredThrowsGeneratorTests
    {
        [Test]
        public void Generator_RecordsEscapingThrow_ForDownstreamCaller()
        {
            const string library = """
                namespace Library;
                public class Resource
                {
                    public Resource(string name)
                    {
                        if (name is null)
                        {
                            throw new System.ArgumentNullException(nameof(name));
                        }

                        throw new System.InvalidOperationException();
                    }
                }
                """;
            MetadataReference reference = emit(library, out string? generated);
            Assert.That(generated, Does.Contain("System.ArgumentNullException"));
            Assert.That(generated, Does.Contain("System.InvalidOperationException"));
            Assert.That(generated, Does.Contain("M:Library.Resource.#ctor(System.String)"));

            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                """
                class Caller
                {
                    void M()
                    {
                        _ = new Library.Resource("a");
                    }
                }
                """,
                additionalReferences: [reference]);

            string[] messages = diagnostics
                .Where(item => item.Id == "EXC003")
                .Select(item => item.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(2));
            Assert.That(messages, Has.Some.Contains("System.ArgumentNullException"));
            Assert.That(messages, Has.Some.Contains("System.InvalidOperationException"));
        }

        [Test]
        public void Generator_ExplicitThrows_OverridesRecordedContract()
        {
            const string library = """
                using Exceptionable;
                namespace Library;
                public class Resource
                {
                    [Throws("System.IO.IOException")]
                    public void Execute(bool fail)
                    {
                        if (fail)
                        {
                            throw new System.IO.IOException();
                        }

                        throw new System.InvalidOperationException();
                    }
                }
                """;
            MetadataReference reference = emit(library, out string? generated);
            Assert.That(generated, Is.Null);

            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                """
                class Caller
                {
                    void M(Library.Resource resource)
                    {
                        resource.Execute(false);
                    }
                }
                """,
                additionalReferences: [reference]);

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.IO.IOException"));
            Assert.That(diagnostics.Select(item => item.GetMessage()), Has.No.Contains("System.InvalidOperationException"));
        }

        private static MetadataReference emit(string source, out string? generated)
        {
            CSharpCompilation input = TestCompilation.Create(source)
                .AddReferences(MetadataReference.CreateFromFile(typeof(DoesNotThrowAttribute).Assembly.Location));
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new InferredThrowsGenerator());
            driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);
            Assert.That(generatorDiagnostics.Where(item => item.Severity == DiagnosticSeverity.Error), Is.Empty);
            string[] sources = driver.GetRunResult().Results
                .SelectMany(result => result.GeneratedSources)
                .Select(source => source.SourceText.ToString())
                .ToArray();
            generated = sources.Length == 0 ? null : string.Join("\n", sources);

            string[] errors = output.GetDiagnostics()
                .Where(item => item.Severity == DiagnosticSeverity.Error)
                .Select(item => item.ToString())
                .ToArray();
            Assert.That(errors, Is.Empty, "Generated inferred contracts must compile.");

            using var assembly = new MemoryStream();
            Microsoft.CodeAnalysis.Emit.EmitResult emitted = output.Emit(assembly);
            Assert.That(emitted.Success, Is.True, string.Join("\n", emitted.Diagnostics));
            assembly.Position = 0;
            return MetadataReference.CreateFromImage(assembly.ToArray());
        }
    }
}