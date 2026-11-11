using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Exceptionable.Analyzers.CodeFixes
{
    /// <summary>
    /// Declares the exception that EXC003 reports on the enclosing member with the string form of
    /// <c>[Throws]</c>, which works for every supported target framework.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddThrowsCodeFixProvider)), Shared]
    public sealed class AddThrowsCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ExceptionableDiagnosticIds.CallerViolation];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            if (!diagnostic.Properties.TryGetValue(ExceptionableDiagnosticIds.ExceptionTypeProperty, out string? exceptionType) ||
                string.IsNullOrEmpty(exceptionType) ||
                exceptionType!.Contains('<'))
            {
                // A closed generic type has no string name the analyzer can resolve.
                return;
            }

            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxNode? member = root is null ? null : findDeclaringMember(root.FindNode(context.Span));
            if (member is null ||
                hasDoesNotThrow(member) ||
                !await hasThrowsAttributeAsync(context.Document, context.CancellationToken).ConfigureAwait(false))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Declare [Throws(\"{exceptionType}\")]",
                    cancellationToken => addAttributeAsync(context.Document, member, exceptionType!, cancellationToken),
                    equivalenceKey: nameof(AddThrowsCodeFixProvider) + ":" + exceptionType),
                diagnostic);
        }

        // The analyzer ignores lambdas and local functions, so the nearest member is the contract owner.
        private static SyntaxNode? findDeclaringMember(SyntaxNode node)
        {
            foreach (SyntaxNode ancestor in node.AncestorsAndSelf())
            {
                switch (ancestor)
                {
                    case LocalFunctionStatementSyntax:
                    case AnonymousFunctionExpressionSyntax:
                        return null;
                    case BaseMethodDeclarationSyntax:
                    case AccessorDeclarationSyntax:
                        return ancestor;
                }
            }

            return null;
        }

        private static async Task<bool> hasThrowsAttributeAsync(Document document, CancellationToken cancellationToken)
        {
            Compilation? compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            return compilation?.GetTypeByMetadataName("Exceptionable.ThrowsAttribute") is not null;
        }

        private static bool hasDoesNotThrow(SyntaxNode member) =>
            getAttributeLists(member)
                .SelectMany(list => list.Attributes)
                .Any(attribute =>
                {
                    string name = attribute.Name.ToString();
                    return name.EndsWith("DoesNotThrow", StringComparison.Ordinal) ||
                        name.EndsWith("DoesNotThrowAttribute", StringComparison.Ordinal);
                });

        private static SyntaxList<AttributeListSyntax> getAttributeLists(SyntaxNode member) =>
            member switch
            {
                BaseMethodDeclarationSyntax method => method.AttributeLists,
                AccessorDeclarationSyntax accessor => accessor.AttributeLists,
                _ => default,
            };

        private static async Task<Document> addAttributeAsync(
            Document document,
            SyntaxNode member,
            string exceptionType,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            AttributeListSyntax attributeList = SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.ParseName("global::Exceptionable.Throws")
                                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation),
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(exceptionType))))))))
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode updatedMember = member switch
            {
                BaseMethodDeclarationSyntax method => addToMethod(method, attributeList),
                AccessorDeclarationSyntax accessor => addToAccessor(accessor, attributeList),
                _ => member,
            };

            return document.WithSyntaxRoot(root.ReplaceNode(member, updatedMember));
        }

        private static SyntaxNode addToMethod(BaseMethodDeclarationSyntax method, AttributeListSyntax attributeList)
        {
            if (method.AttributeLists.Count > 0)
            {
                return method.WithAttributeLists(method.AttributeLists.Add(
                    attributeList.WithLeadingTrivia(method.AttributeLists.Last().GetLeadingTrivia())));
            }

            // The new attribute takes the declaration's leading comments and indentation; the declaration keeps only its indentation.
            SyntaxTriviaList leading = method.GetLeadingTrivia();
            SyntaxTrivia indentation = leading.LastOrDefault(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));
            return method
                .WithLeadingTrivia(indentation)
                .WithAttributeLists(SyntaxFactory.SingletonList(
                    attributeList
                        .WithLeadingTrivia(leading)
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)));
        }

        private static SyntaxNode addToAccessor(AccessorDeclarationSyntax accessor, AttributeListSyntax attributeList) =>
            accessor.AttributeLists.Count > 0
                ? accessor.WithAttributeLists(accessor.AttributeLists.Add(attributeList))
                : accessor
                    .WithLeadingTrivia(SyntaxFactory.ElasticSpace)
                    .WithAttributeLists(SyntaxFactory.SingletonList(
                        attributeList
                            .WithLeadingTrivia(accessor.GetLeadingTrivia())
                            .WithTrailingTrivia(SyntaxFactory.ElasticSpace)));
    }
}