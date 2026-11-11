using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    /// <summary>
    /// Reads the throws that the inferred-throws generator recorded as assembly
    /// metadata for members of a referenced assembly.
    /// </summary>
    internal sealed class RecordedThrowsReader
    {
        private readonly Compilation compilation;
        private readonly ExceptionTypeResolver exceptionTypes;
        private readonly ConcurrentDictionary<IAssemblySymbol, ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>>> recorded =
            new(SymbolEqualityComparer.Default);

        internal RecordedThrowsReader(Compilation compilation, ExceptionTypeResolver exceptionTypes)
        {
            this.compilation = compilation;
            this.exceptionTypes = exceptionTypes;
        }

        internal ImmutableArray<INamedTypeSymbol> Read(IMethodSymbol method)
        {
            string? memberId = method.GetDocumentationCommentId();
            if (memberId is null)
            {
                return [];
            }

            ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>> map =
                this.recorded.GetOrAdd(method.ContainingAssembly, this.readAssembly);
            return map.TryGetValue(memberId, out ImmutableArray<INamedTypeSymbol> types)
                ? types
                : [];
        }

        private ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>> readAssembly(IAssemblySymbol assembly)
        {
            var builder = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
            foreach (AttributeData attribute in assembly.GetAttributes())
            {
                if (!InferredThrowsMetadata.IsInferredThrows(attribute) ||
                    attribute.ConstructorArguments.Length < 2 ||
                    attribute.ConstructorArguments[0].Value is not string memberId ||
                    attribute.ConstructorArguments[1].Value is not string exceptionName)
                {
                    continue;
                }

                INamedTypeSymbol? exceptionType = InferredThrowsMetadata.ResolveExceptionType(
                    exceptionName,
                    this.compilation,
                    this.exceptionTypes);
                if (exceptionType is null)
                {
                    continue;
                }

                if (!builder.TryGetValue(memberId, out List<INamedTypeSymbol>? types))
                {
                    types = [];
                    builder.Add(memberId, types);
                }

                if (!types.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, exceptionType)))
                {
                    types.Add(exceptionType);
                }
            }

            var result = ImmutableDictionary.CreateBuilder<string, ImmutableArray<INamedTypeSymbol>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<INamedTypeSymbol>> entry in builder)
            {
                result.Add(entry.Key, entry.Value.ToImmutableArray());
            }

            return result.ToImmutable();
        }
    }
}
