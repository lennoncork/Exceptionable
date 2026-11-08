using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
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
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsEscapingThrow_ForDownstreamCaller.Source1");
            MetadataReference reference = emit(library, out _);
            Assert.That(
                readRecorded(reference),
                Is.EqualTo(new (string MemberId, string ExceptionType)[]
                {
                    ("M:Library.Resource.#ctor(System.String)", "System.ArgumentNullException"),
                    ("M:Library.Resource.#ctor(System.String)", "System.InvalidOperationException"),
                }));

            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsEscapingThrow_ForDownstreamCaller.Source2"),
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
        public void Generator_ResolvesClosedGenericExceptionTypesAcrossAssemblies()
        {
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_ResolvesClosedGenericExceptionTypesAcrossAssemblies.Source3");
            MetadataReference reference = emit(library, out _);
            (string MemberId, string ExceptionType)[] records = readRecorded(reference);
            Assert.That(records, Has.Length.EqualTo(3));
            Assert.That(records.Select(record => record.ExceptionType),
                Has.All.StartsWith("exceptionable-type-v1:"));
            CSharpCompilation consumer = TestCompilation.Create(string.Empty)
                .WithAssemblyName("InferredThrowsGenericConsumer")
                .AddReferences(reference);
            ExceptionTypeResolver resolver = new(consumer);
            foreach ((string _, string exceptionType) in records)
            {
                Assert.That(
                    InferredThrowsMetadata.ResolveExceptionType(exceptionType, consumer, resolver),
                    Is.Not.Null,
                    exceptionType);
            }

            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_ResolvesClosedGenericExceptionTypesAcrossAssemblies.Source4"),
                "Library.GenericException<int>");
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_ResolvesClosedGenericExceptionTypesAcrossAssemblies.Source5"),
                "Library.Outer<string>.NestedException<int>");
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_ResolvesClosedGenericExceptionTypesAcrossAssemblies.Source6"),
                "Library.GenericException<int[]>");
        }

        [Test]
        public void Generator_AvoidsConflictingHelperAttribute()
        {
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_AvoidsConflictingHelperAttribute.Source7");

            MetadataReference reference = emit(library, out string? generated);
            Assert.That(generated, Does.Contain("InferredThrowsAttribute2"));
            CSharpCompilation reader = TestCompilation.Create(string.Empty)
                .WithAssemblyName("CollisionReader")
                .AddReferences(reference);
            IAssemblySymbol assembly = (IAssemblySymbol)reader.GetAssemblyOrModuleSymbol(reference)!;
            AttributeData generatedAttribute = assembly.GetAttributes()
                .Single(attribute => attribute.AttributeClass?.Name == "InferredThrowsAttribute2");
            Assert.That(InferredThrowsMetadata.IsInferredThrows(generatedAttribute), Is.True);
            Assert.That(readRecorded(reference), Has.Length.EqualTo(1));
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_AvoidsConflictingHelperAttribute.Source8"),
                "System.InvalidOperationException");
        }

        [Test]
        public void Generator_RecordsOnlyMembersAccessibleOutsidePrivateScopes()
        {
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsOnlyMembersAccessibleOutsidePrivateScopes.Source9");
            MetadataReference reference = emit(library, out _);

            Assert.That(
                readRecorded(reference).Select(record => record.MemberId),
                Is.EquivalentTo(new[]
                {
                    "M:Resource.Public",
                    "M:Resource.Protected",
                    "M:Resource.Internal",
                    "M:Resource.PublicNested.Visible",
                }));

            Diagnostic[] friendDiagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsOnlyMembersAccessibleOutsidePrivateScopes.Source10"),
                additionalReferences: [reference],
                assemblyName: "ExceptionableTests");
            Assert.That(friendDiagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("EXC003"));
        }

        [Test]
        public void Generator_DoesNotRecordInternalMembersWithoutFriendAssemblies()
        {
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_DoesNotRecordInternalMembersWithoutFriendAssemblies.Source11");

            MetadataReference reference = emit(library, out _);

            Assert.That(readRecorded(reference), Is.Empty);
        }

        [Test]
        public void Generator_NullableGuardOnPublicApiIsNotRuntimeValidationForObliviousCallers()
        {
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_NullableGuardOnPublicApiIsNotRuntimeValidationForObliviousCallers.Source12");
            MetadataReference reference = emit(library, out _);
            Assert.That(readRecorded(reference), Is.Empty);

            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase("Generator_NullableGuardOnPublicApiIsNotRuntimeValidationForObliviousCallers.Source13"),
                additionalReferences: [reference]);

            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Does.Not.Contain("EXC003"));
        }

        [Test]
        public void Generator_ExplicitThrows_OverridesRecordedContract()
        {
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_ExplicitThrows_OverridesRecordedContract.Source14");
            MetadataReference reference = emit(library, out string? generated);
            Assert.That(generated, Is.Null);
            Assert.That(readRecorded(reference), Is.Empty);

            Diagnostic[] diagnostics = ExceptionableAnalyzerTests.GetDiagnostics(
                ExceptionableAnalyzerTests.GetTestCase("Generator_ExplicitThrows_OverridesRecordedContract.Source15"),
                additionalReferences: [reference]);

            Diagnostic diagnostic = diagnostics.Single(item => item.Id == "EXC003");
            Assert.That(diagnostic.GetMessage(), Does.Contain("System.IO.IOException"));
            Assert.That(diagnostics.Select(item => item.GetMessage()), Has.No.Contains("System.InvalidOperationException"));
        }

        [Test]
        public void Generator_RecordsMemberSpecificContracts_FromEmittedAssembly()
        {
            string library = ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsMemberSpecificContracts_FromEmittedAssembly.Source16");
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
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsMemberSpecificContracts_FromEmittedAssembly.Source17"),
                "System.IO.IOException",
                "System.ObjectDisposedException");
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsMemberSpecificContracts_FromEmittedAssembly.Source18"),
                "System.InvalidOperationException");
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsMemberSpecificContracts_FromEmittedAssembly.Source19"),
                "System.TimeoutException");
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsMemberSpecificContracts_FromEmittedAssembly.Source20"));
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsMemberSpecificContracts_FromEmittedAssembly.Source21"));
            assertCaller(
                reference,
                ExceptionableAnalyzerTests.GetTestCase("Generator_RecordsMemberSpecificContracts_FromEmittedAssembly.Source22"));
        }

        private static void assertCaller(
            MetadataReference reference,
            string caller,
            params string[] exceptionNames)
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
                .Where(InferredThrowsMetadata.IsInferredThrows)
                .Select(attribute => (
                    MemberId: (string)attribute.ConstructorArguments[0].Value!,
                    ExceptionType: (string)attribute.ConstructorArguments[1].Value!))
                .OrderBy(record => record.MemberId, StringComparer.Ordinal)
                .ThenBy(record => record.ExceptionType, StringComparer.Ordinal)];
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