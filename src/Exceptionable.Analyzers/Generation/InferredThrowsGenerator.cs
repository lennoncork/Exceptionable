using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Exceptionable.Analyzers.Analysis;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Exceptionable.Analyzers.Generation
{
    /// <summary>
    /// Records escaping throws as assembly metadata so a referencing compilation can
    /// see the inferred [Throws] contract. An explicit [Throws] or [DoesNotThrow]
    /// contract suppresses the record.
    /// </summary>
    [Generator]
    public sealed class InferredThrowsGenerator : IIncrementalGenerator
    {
        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ISymbol?> symbols = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ThrowStatementSyntax or ThrowExpressionSyntax,
                static (syntaxContext, cancellationToken) =>
                    syntaxContext.SemanticModel.GetEnclosingSymbol(syntaxContext.Node.SpanStart, cancellationToken));
            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<ISymbol?> Symbols)> combined =
                context.CompilationProvider.Combine(symbols.Collect());
            context.RegisterSourceOutput(combined, static (production, source) =>
                emit(production, source.Compilation, source.Symbols));
        }

        private static void emit(
            SourceProductionContext production,
            Compilation compilation,
            ImmutableArray<ISymbol?> symbols)
        {
            var analysis = new InferredThrowAnalysis(compilation, new ReachabilityAnalysis());
            var contracts = new ContractResolver(new ExceptionTypeResolver(compilation));
            var seen = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var entries = new List<(string MemberId, string ExceptionType)>();
            foreach (ISymbol? symbol in symbols)
            {
                foreach (IMethodSymbol method in expand(symbol))
                {
                    if (!seen.Add(method) ||
                        !isExternallyConsumable(method, compilation) ||
                        contracts.GetEffectiveContract(method).HasContract)
                    {
                        continue;
                    }

                    string? memberId = method.GetDocumentationCommentId();
                    if (memberId is null)
                    {
                        continue;
                    }

                    foreach (INamedTypeSymbol exceptionType in analysis.GetImplicitDeclaredTypes(method))
                    {
                        entries.Add((memberId, InferredThrowsMetadata.GetExceptionTypeName(exceptionType)));
                    }
                }
            }

            if (entries.Count == 0)
            {
                return;
            }

            string attributeName = getAttributeName(compilation);
            entries.Sort(static (left, right) =>
            {
                int member = string.CompareOrdinal(left.MemberId, right.MemberId);
                return member != 0
                    ? member
                    : string.CompareOrdinal(left.ExceptionType, right.ExceptionType);
            });
            production.AddSource(
                "Exceptionable.InferredThrowsAttribute.g.cs",
                SourceText.From(renderAttributeDefinition(attributeName), Encoding.UTF8));
            production.AddSource(
                "Exceptionable.InferredThrows.g.cs",
                SourceText.From(renderApplications(entries, attributeName), Encoding.UTF8));
        }

        private static bool isExternallyConsumable(IMethodSymbol method, Compilation compilation)
        {
            bool hasFriendAssembly = hasInternalsVisibleTo(compilation.Assembly);
            if (isPrivate(method.DeclaredAccessibility) ||
                requiresFriend(method.DeclaredAccessibility) && !hasFriendAssembly)
            {
                return false;
            }

            for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.ContainingType)
            {
                if (isPrivate(type.DeclaredAccessibility) ||
                    requiresFriend(type.DeclaredAccessibility) && !hasFriendAssembly)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool isPrivate(Accessibility accessibility) =>
            accessibility == Accessibility.Private;

        private static bool requiresFriend(Accessibility accessibility) =>
            accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal;

        private static bool hasInternalsVisibleTo(IAssemblySymbol assembly)
        {
            foreach (AttributeData attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() ==
                        "System.Runtime.CompilerServices.InternalsVisibleToAttribute" &&
                    attribute.ConstructorArguments.Length > 0 &&
                    attribute.ConstructorArguments[0].Value is string)
                {
                    return true;
                }
            }

            return false;
        }

        private static string getAttributeName(Compilation compilation)
        {
            string name = InferredThrowsMetadata.AttributeName;
            int suffix = 0;
            while (compilation.GetTypeByMetadataName(
                InferredThrowsMetadata.Namespace + "." + name) is not null)
            {
                suffix++;
                name = InferredThrowsMetadata.AttributeName + suffix;
            }

            return name;
        }

        private static IEnumerable<IMethodSymbol> expand(ISymbol? symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol method:
                    yield return method;
                    yield break;
                case IFieldSymbol field:
                    foreach (IMethodSymbol constructor in initializers(field.ContainingType, field.IsStatic))
                    {
                        yield return constructor;
                    }

                    yield break;
                case IPropertySymbol property:
                    if (property.GetMethod is not null)
                    {
                        yield return property.GetMethod;
                    }

                    if (property.SetMethod is not null)
                    {
                        yield return property.SetMethod;
                    }

                    foreach (IMethodSymbol constructor in initializers(property.ContainingType, property.IsStatic))
                    {
                        yield return constructor;
                    }

                    yield break;
            }
        }

        private static IEnumerable<IMethodSymbol> initializers(INamedTypeSymbol type, bool isStatic)
        {
            ImmutableArray<IMethodSymbol> constructors = isStatic ? type.StaticConstructors : type.Constructors;
            foreach (IMethodSymbol constructor in constructors)
            {
                if (ConstructorHost.RunsInitializers(constructor))
                {
                    yield return constructor;
                }
            }
        }

        private const string attributeDefinition = """
            // <auto-generated/>
            #nullable disable
            using System;

            namespace Exceptionable.Generation
            {
                [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
                internal sealed class __ATTRIBUTE_NAME__ : Attribute
                {
                    public __ATTRIBUTE_NAME__(string memberId, string exceptionType__MARKER_PARAMETER__)
                    {
                        this.MemberId = memberId;
                        this.ExceptionType = exceptionType;
                    }

                    public string MemberId { get; }

                    public string ExceptionType { get; }
                }
            }
            """;

        private static string renderAttributeDefinition(string attributeName) =>
            attributeDefinition
                .Replace("__ATTRIBUTE_NAME__", attributeName)
                .Replace(
                    "__MARKER_PARAMETER__",
                    attributeName == InferredThrowsMetadata.AttributeName
                        ? string.Empty
                        : ", string marker");

        private static string renderApplications(
            List<(string MemberId, string ExceptionType)> entries,
            string attributeName)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            foreach ((string memberId, string exceptionType) in entries)
            {
                builder.Append("[assembly: global::Exceptionable.Generation.");
                builder.Append(attributeName);
                builder.Append('(');
                builder.Append(literal(memberId));
                builder.Append(", ");
                builder.Append(literal(exceptionType));
                if (attributeName != InferredThrowsMetadata.AttributeName)
                {
                    builder.Append(", ");
                    builder.Append(literal(InferredThrowsMetadata.MarkerValue));
                }

                builder.AppendLine(")]");
            }

            return builder.ToString();
        }

        private static string literal(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}