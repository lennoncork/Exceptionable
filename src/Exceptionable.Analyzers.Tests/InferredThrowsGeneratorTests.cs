using System;
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
            MetadataReference reference = emit(library, out _);
            Assert.That(
                readRecorded(reference),
                Is.EqualTo(new (string MemberId, string ExceptionType)[]
                {
                    ("M:Library.Resource.#ctor(System.String)", "System.ArgumentNullException"),
                    ("M:Library.Resource.#ctor(System.String)", "System.InvalidOperationException"),
                }));

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
            Assert.That(readRecorded(reference), Is.Empty);

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

        [Test]
        public void Generator_RecordsMemberSpecificContracts_FromEmittedAssembly()
        {
            const string library = """
                #nullable enable
                using Exceptionable;
                namespace Library;
                public class Resource
                {
                    public int field = 0 switch { _ => throw new System.IO.IOException() };

                    public int Initialized { get; } = 0 switch { _ => throw new System.ObjectDisposedException("Initialized") };

                    public int Value
                    {
                        get => throw new System.InvalidOperationException();
                    }

                    public void Execute()
                    {
                        throw new System.TimeoutException();
                    }

                    public void Caught()
                    {
                        try
                        {
                            throw new System.ArgumentException();
                        }
                        catch (System.ArgumentException)
                        {
                        }
                    }

                    [DoesNotThrow]
                    public void Suppressed()
                    {
                        throw new System.NotSupportedException();
                    }

                    public Resource(string name)
                    {
                        if (name is null)
                        {
                            throw new System.ArgumentNullException(nameof(name));
                        }
                    }
                }
                """;
            MetadataReference reference = emit(library, out _);
            Assert.That(
                readRecorded(reference),
                Is.EqualTo(new (string MemberId, string ExceptionType)[]
                {
                    ("M:Library.Resource.#ctor(System.String)", "System.IO.IOException"),
                    ("M:Library.Resource.#ctor(System.String)", "System.ObjectDisposedException"),
                    ("M:Library.Resource.Execute", "System.TimeoutException"),
                    ("M:Library.Resource.get_Value", "System.InvalidOperationException"),
                }));

            assertCaller(
                reference,
                """
                class Caller
                {
                    void M()
                    {
                        _ = new Library.Resource("a");
                    }
                }
                """,
                "System.IO.IOException",
                "System.ObjectDisposedException");
            assertCaller(
                reference,
                """
                class Caller
                {
                    void M(Library.Resource resource)
                    {
                        _ = resource.Value;
                    }
                }
                """,
                "System.InvalidOperationException");
            assertCaller(
                reference,
                """
                class Caller
                {
                    void M(Library.Resource resource)
                    {
                        resource.Execute();
                    }
                }
                """,
                "System.TimeoutException");
            assertCaller(
                reference,
                """
                class Caller
                {
                    void M(Library.Resource resource)
                    {
                        try
                        {
                            resource.Execute();
                        }
                        catch (System.TimeoutException)
                        {
                        }
                    }
                }
                """);
            assertCaller(
                reference,
                """
                using Exceptionable;
                class Caller
                {
                    [Throws("System.TimeoutException")]
                    void M(Library.Resource resource)
                    {
                        resource.Execute();
                    }
                }
                """);
            assertCaller(
                reference,
                """
                class Caller
                {
                    void M(Library.Resource resource)
                    {
                        resource.Caught();
                        resource.Suppressed();
                    }
                }
                """);
        }

        private static void assertCaller(MetadataReference reference, string caller, params string[] exceptionNames)
        {
            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                caller,
                additionalReferences: [reference]);
            string[] messages = diagnostics
                .Where(item => item.Id == "EXC003")
                .Select(item => item.GetMessage())
                .ToArray();
            Assert.That(messages, Has.Length.EqualTo(exceptionNames.Length));
            foreach (string exceptionName in exceptionNames)
            {
                Assert.That(messages, Has.Some.Contains(exceptionName));
            }
        }

        private static (string MemberId, string ExceptionType)[] readRecorded(MetadataReference reference)
        {
            MetadataReference[] references = [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path)), reference];
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InferredThrowsReader",
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            IAssemblySymbol assembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference)!;
            return [.. assembly.GetAttributes()
                .Where(attribute =>
                    attribute.AttributeClass?.Name == "InferredThrowsAttribute" &&
                    attribute.AttributeClass.ContainingNamespace.ToDisplayString() == "Exceptionable.Generation")
                .Select(attribute => (
                    MemberId: (string)attribute.ConstructorArguments[0].Value!,
                    ExceptionType: (string)attribute.ConstructorArguments[1].Value!))];
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