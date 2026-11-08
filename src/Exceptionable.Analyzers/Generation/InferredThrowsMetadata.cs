using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Generation
{
    internal static class InferredThrowsMetadata
    {
        internal const string Namespace = "Exceptionable.Generation";

        internal const string AttributeName = "InferredThrowsAttribute";

        internal static bool IsInferredThrows(AttributeData attribute)
        {
            INamedTypeSymbol? type = attribute.AttributeClass;
            return type?.Name == AttributeName &&
                type.ContainingNamespace?.ToDisplayString() == Namespace;
        }
    }
}