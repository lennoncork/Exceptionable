using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Contracts
{
    internal static class InferredThrowsMetadata
    {
        internal const string Namespace = "Exceptionable.Generation";

        internal const string AttributeName = "InferredThrowsAttribute";

        internal const string MarkerValue = "exceptionable-inferred-throws-v1";
        private const string typePrefix = "exceptionable-type-v1:";

        internal static string GetExceptionTypeName(INamedTypeSymbol type)
        {
            if (!hasGenericType(type))
            {
                return type.ToDisplayString();
            }

            var builder = new StringBuilder(typePrefix);
            return writeType(builder, type) ? builder.ToString() : type.ToDisplayString();
        }

        internal static INamedTypeSymbol? ResolveExceptionType(
            string value,
            Compilation compilation,
            ExceptionTypeResolver fallback)
        {
            if (!value.StartsWith(typePrefix, StringComparison.Ordinal))
            {
                return fallback.ResolveExceptionType(value);
            }

            var reader = new TypeReader(value.Substring(typePrefix.Length), compilation);
            return reader.ReadType() is INamedTypeSymbol type &&
                reader.AtEnd &&
                fallback.IsExceptionType(type)
                    ? type
                    : null;
        }

        internal static bool IsInferredThrows(AttributeData attribute)
        {
            INamedTypeSymbol? type = attribute.AttributeClass;
            if (type?.ContainingNamespace?.ToDisplayString() != Namespace)
            {
                return false;
            }

            if (type.Name == AttributeName)
            {
                return true;
            }

            if (!type.Name.StartsWith(AttributeName, StringComparison.Ordinal) ||
                !int.TryParse(type.Name.Substring(AttributeName.Length), NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return false;
            }

            return attribute.ConstructorArguments.Length == 3 &&
                attribute.ConstructorArguments[2].Value as string == MarkerValue;
        }

        private static bool hasGenericType(INamedTypeSymbol type) =>
            type.IsGenericType || type.ContainingType is not null && hasGenericType(type.ContainingType);

        private static bool writeType(StringBuilder builder, ITypeSymbol type)
        {
            switch (type)
            {
                case IDynamicTypeSymbol:
                    builder.Append('D');
                    return true;
                case IArrayTypeSymbol array:
                    builder.Append('A');
                    writeField(builder, array.Rank.ToString(CultureInfo.InvariantCulture));
                    writeField(builder, array.IsSZArray ? "1" : "0");
                    return writeType(builder, array.ElementType);
                case INamedTypeSymbol named when named.TypeKind != TypeKind.Error:
                    string containingType = named.ContainingType is null
                        ? string.Empty
                        : encodeType(named.ContainingType);
                    if (named.ContainingType is not null && containingType.Length == 0)
                    {
                        return false;
                    }

                    builder.Append('N');
                    writeField(builder, named.ContainingAssembly.Identity.ToString());
                    writeField(builder, named.ContainingType is null
                        ? getNamespaceMetadataName(named) + named.MetadataName
                        : named.MetadataName);
                    writeField(builder, containingType);
                    writeField(builder, named.TypeArguments.Length.ToString(CultureInfo.InvariantCulture));
                    foreach (ITypeSymbol argument in named.TypeArguments)
                    {
                        if (!writeType(builder, argument))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return false;
            }
        }

        private static string encodeType(ITypeSymbol type)
        {
            var builder = new StringBuilder();
            return writeType(builder, type) ? builder.ToString() : string.Empty;
        }

        private static string getNamespaceMetadataName(INamedTypeSymbol type)
        {
            var parts = new Stack<string>();
            for (INamespaceSymbol? current = type.ContainingNamespace;
                current is not null && !current.IsGlobalNamespace;
                current = current.ContainingNamespace)
            {
                parts.Push(current.MetadataName);
            }

            return parts.Count == 0 ? string.Empty : string.Join(".", parts) + ".";
        }

        private static void writeField(StringBuilder builder, string value)
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        private sealed class TypeReader
        {
            private readonly string value;
            private readonly Compilation compilation;
            private int position;

            internal TypeReader(string value, Compilation compilation)
            {
                this.value = value;
                this.compilation = compilation;
            }

            internal bool AtEnd => this.position == this.value.Length;

            internal ITypeSymbol? ReadType()
            {
                if (this.position >= this.value.Length)
                {
                    return null;
                }

                char kind = this.value[this.position++];
                if (kind == 'D')
                {
                    return this.compilation.DynamicType;
                }

                if (kind == 'A')
                {
                    if (!int.TryParse(this.readField(), NumberStyles.None, CultureInfo.InvariantCulture, out int rank) ||
                        !int.TryParse(this.readField(), NumberStyles.None, CultureInfo.InvariantCulture, out int isSzArray) ||
                        rank is < 1 or > 32 ||
                        isSzArray is not (0 or 1) ||
                        (rank == 1 && isSzArray == 0))
                    {
                        return null;
                    }

                    ITypeSymbol? elementType = this.ReadType();
                    return elementType is null
                        ? null
                        : this.compilation.CreateArrayTypeSymbol(elementType, rank);
                }

                if (kind != 'N')
                {
                    return null;
                }

                string assemblyIdentity = this.readField();
                string metadataName = this.readField();
                string containingTypeName = this.readField();
                if (!int.TryParse(this.readField(), NumberStyles.None, CultureInfo.InvariantCulture, out int argumentCount) ||
                    argumentCount < 0 ||
                    argumentCount > this.value.Length - this.position)
                {
                    return null;
                }

                IAssemblySymbol? assembly = this.findAssembly(assemblyIdentity);
                if (assembly is null)
                {
                    return null;
                }

                INamedTypeSymbol? containingType = null;
                INamedTypeSymbol? definition;
                if (containingTypeName.Length == 0)
                {
                    definition = assembly.GetTypeByMetadataName(metadataName);
                }
                else
                {
                    var containingReader = new TypeReader(containingTypeName, this.compilation);
                    containingType = containingReader.ReadType() as INamedTypeSymbol;
                    if (!containingReader.AtEnd)
                    {
                        return null;
                    }

                    definition = containingType?.GetTypeMembers()
                        .FirstOrDefault(candidate => candidate.MetadataName == metadataName);
                }

                if (definition is null)
                {
                    return null;
                }

                ITypeSymbol[] arguments = new ITypeSymbol[argumentCount];
                for (int index = 0; index < argumentCount; index++)
                {
                    ITypeSymbol? argument = this.ReadType();
                    if (argument is null)
                    {
                        return null;
                    }

                    arguments[index] = argument;
                }

                if (argumentCount == 0)
                {
                    return definition;
                }

                return definition.Arity == argumentCount
                    ? definition.Construct(arguments)
                    : null;
            }

            private IAssemblySymbol? findAssembly(string identity)
            {
                if (this.compilation.Assembly.Identity.ToString() == identity)
                {
                    return this.compilation.Assembly;
                }

                foreach (MetadataReference reference in this.compilation.References)
                {
                    if (this.compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly &&
                        assembly.Identity.ToString() == identity)
                    {
                        return assembly;
                    }
                }

                return null;
            }

            private string readField()
            {
                if (this.position < 0 || this.position >= this.value.Length)
                {
                    this.position = this.value.Length + 1;
                    return string.Empty;
                }

                int separator = this.value.IndexOf(':', this.position);
                if (separator < 0 ||
                    !int.TryParse(this.value.Substring(this.position, separator - this.position), NumberStyles.None, CultureInfo.InvariantCulture, out int length) ||
                    length < 0 ||
                    length > this.value.Length - separator - 1)
                {
                    this.position = this.value.Length + 1;
                    return string.Empty;
                }

                int start = separator + 1;
                this.position = start + length;
                return this.value.Substring(start, length);
            }
        }
    }
}