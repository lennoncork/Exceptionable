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

namespace Exceptionable.Analyzers.CodeFixes
{
    /// <summary>
    /// Copies an interface member's exception contract onto an implementation that does not declare one.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MatchInterfaceContractCodeFixProvider)), Shared]
    public sealed class MatchInterfaceContractCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ExceptionableDiagnosticIds.InterfaceContractMismatch];

        public override FixAllProvider? GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            if (!diagnostic.Properties.TryGetValue(ExceptionableDiagnosticIds.ContractKindProperty, out string? kind) ||
                kind is not (ExceptionableDiagnosticIds.DoesNotThrowContract or ExceptionableDiagnosticIds.ThrowsContract))
            {
                return;
            }

            string[] exceptionTypes = kind == ExceptionableDiagnosticIds.ThrowsContract
                ? (diagnostic.Properties[ExceptionableDiagnosticIds.ExceptionTypesProperty] ?? string.Empty)
                    .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                : [];
            if (kind == ExceptionableDiagnosticIds.ThrowsContract &&
                (exceptionTypes.Length == 0 || exceptionTypes.Any(type => type.Contains('<'))))
            {
                return;
            }

            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxNode? member = root is null ? null : findMember(root.FindNode(context.Span));
            if (member is null)
            {
                return;
            }

            string title = kind == ExceptionableDiagnosticIds.DoesNotThrowContract
                ? "Declare [DoesNotThrow]"
                : "Declare the interface [Throws] contract";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => addContractAsync(context.Document, member, kind, exceptionTypes, cancellationToken),
                    equivalenceKey: nameof(MatchInterfaceContractCodeFixProvider) + ":" + kind),
                diagnostic);
        }

        private static SyntaxNode? findMember(SyntaxNode node)
        {
            foreach (SyntaxNode ancestor in node.AncestorsAndSelf())
            {
                switch (ancestor)
                {
                    case BaseMethodDeclarationSyntax:
                    case PropertyDeclarationSyntax:
                    case EventDeclarationSyntax:
                    case IndexerDeclarationSyntax:
                        return ancestor;
                }
            }

            return null;
        }

        private static async Task<Document> addContractAsync(
            Document document,
            SyntaxNode member,
            string kind,
            string[] exceptionTypes,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            AttributeListSyntax[] lists = kind == ExceptionableDiagnosticIds.DoesNotThrowContract
                ? [attributeList("global::Exceptionable.DoesNotThrow", argument: null)]
                : exceptionTypes.Select(type => attributeList("global::Exceptionable.Throws", type)).ToArray();
            SyntaxNode updated = member;
            foreach (AttributeListSyntax list in lists)
            {
                updated = addList(updated, list);
            }

            return document.WithSyntaxRoot(root.ReplaceNode(member, updated));
        }

        private static AttributeListSyntax attributeList(string name, string? argument)
        {
            AttributeSyntax attribute = argument is null
                ? SyntaxFactory.Attribute(SyntaxFactory.ParseName(name))
                : SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName(name),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(argument))))));
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxNode addList(SyntaxNode member, AttributeListSyntax attributeList)
        {
            return member switch
            {
                BaseMethodDeclarationSyntax method => method.AddAttributeLists(attributeList),
                PropertyDeclarationSyntax property => property.AddAttributeLists(attributeList),
                EventDeclarationSyntax eventDeclaration => eventDeclaration.AddAttributeLists(attributeList),
                IndexerDeclarationSyntax indexer => indexer.AddAttributeLists(attributeList),
                _ => member,
            };
        }
    }
}