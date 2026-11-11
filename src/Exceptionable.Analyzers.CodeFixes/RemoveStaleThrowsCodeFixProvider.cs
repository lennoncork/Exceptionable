using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Exceptionable.Analyzers.CodeFixes
{
    /// <summary>Removes the <c>[Throws]</c> attribute that EXC008 reports as stale.</summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveStaleThrowsCodeFixProvider)), Shared]
    public sealed class RemoveStaleThrowsCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ExceptionableDiagnosticIds.UnusedDeclaredException];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            AttributeSyntax? attribute = root?
                .FindNode(context.Span, getInnermostNodeForTie: true)
                .FirstAncestorOrSelf<AttributeSyntax>();
            if (attribute is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Remove stale [Throws] declaration",
                    cancellationToken => removeAttributeAsync(context.Document, attribute, cancellationToken),
                    equivalenceKey: nameof(RemoveStaleThrowsCodeFixProvider)),
                context.Diagnostics);
        }

        private static async Task<Document> removeAttributeAsync(
            Document document,
            AttributeSyntax attribute,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            SyntaxNode target = attribute.Parent is AttributeListSyntax { Attributes.Count: 1 } list
                ? list
                : attribute;
            SyntaxNode? updated = root.RemoveNode(target, SyntaxRemoveOptions.KeepDirectives);
            return updated is null ? document : document.WithSyntaxRoot(updated);
        }
    }
}