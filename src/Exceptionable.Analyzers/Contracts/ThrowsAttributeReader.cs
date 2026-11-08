using System;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal static class ThrowsAttributeReader
    {
        internal const string DoesNotThrowAttributeName = "Exceptionable.DoesNotThrowAttribute";
        internal const string ThrowsAttributeName = "Exceptionable.ThrowsAttribute";

        internal static bool IsDoesNotThrow(AttributeData attribute) =>
            isAttribute(attribute, DoesNotThrowAttributeName);

        internal static bool IsThrows(AttributeData attribute) =>
            isThrowsAttribute(attribute.AttributeClass);

        internal static bool TryGetThrowsType(AttributeData attribute, out ITypeSymbol? type)
        {
            if (isThrowsAttribute(attribute.AttributeClass) &&
                attribute.AttributeClass is { IsGenericType: true, TypeArguments.Length: 1 } attributeType)
            {
                type = attributeType.TypeArguments[0];
                return true;
            }

            type = null;
            return false;
        }

        internal static bool TryGetThrowsName(AttributeData attribute, out string name)
        {
            if (isThrowsAttribute(attribute.AttributeClass) &&
                attribute.AttributeClass is { IsGenericType: false } &&
                attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string value &&
                !string.IsNullOrWhiteSpace(value))
            {
                name = value.Trim();
                return true;
            }

            name = string.Empty;
            return false;
        }

        private static bool isAttribute(AttributeData attribute, string metadataName) =>
            attribute.AttributeClass?.ToDisplayString() == metadataName;

        private static bool isThrowsAttribute(INamedTypeSymbol? type)
        {
            if (type?.ContainingType is not null)
            {
                return false;
            }

            INamedTypeSymbol? definition = type?.OriginalDefinition;
            return definition is not null &&
                definition.Name == "ThrowsAttribute" &&
                definition.Arity is 0 or 1 &&
                definition.ContainingNamespace?.ToDisplayString() == "Exceptionable";
        }
    }
}