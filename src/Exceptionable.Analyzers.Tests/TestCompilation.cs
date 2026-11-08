using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Exceptionable.Analyzers.Tests
{
    internal static class TestCompilation
    {
        internal static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

        internal static CSharpCompilation Create(string source)
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(System.IO.Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));

            return CSharpCompilation.Create(
                assemblyName: "ExceptionableUnitTests",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, ParseOptions) },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        internal static IMethodSymbol GetMethod(CSharpCompilation compilation, string typeName, string methodName)
        {
            var type = compilation.GetTypeByMetadataName(typeName);
            if (type is null)
            {
                throw new InvalidOperationException($"Could not find type '{typeName}'.");
            }

            var method = type.GetMembers(methodName).OfType<IMethodSymbol>().SingleOrDefault();
            return method ?? throw new InvalidOperationException(
                $"Could not find method '{typeName}.{methodName}'.");
        }
    }
}