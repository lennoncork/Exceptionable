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
            isAttribute(attribute, ThrowsAttributeName);

        internal static string GetThrowsName(AttributeData attribute) =>
            TryGetThrowsName(attribute, out var name) ? name : string.Empty;

        internal static bool TryGetThrowsName(AttributeData attribute, out string name)
        {
            if (attribute.ConstructorArguments.Length > 0 &&
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
    }
}