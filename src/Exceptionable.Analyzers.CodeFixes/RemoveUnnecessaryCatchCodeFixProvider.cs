using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Exceptionable.Analyzers.CodeFixes
{
    /// <summary>
    /// Removes catch clauses that EXC006 proved cannot observe a tracked exception. A
    /// <c>finally</c> block stays in place.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUnnecessaryCatchCodeFixProvider)), Shared]
    public sealed class RemoveUnnecessaryCatchCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ExceptionableDiagnosticIds.UnnecessaryTryCatch];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            TryStatementSyntax? tryStatement = root?
                .FindNode(context.Span, getInnermostNodeForTie: true)
                .FirstAncestorOrSelf<TryStatementSyntax>();
            if (tryStatement is null || tryStatement.Catches.Count == 0)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    tryStatement.Finally is null
                        ? "Remove unnecessary try/catch"
                        : "Remove unnecessary catch",
                    cancellationToken => removeCatchAsync(context.Document, tryStatement, cancellationToken),
                    equivalenceKey: nameof(RemoveUnnecessaryCatchCodeFixProvider)),
                context.Diagnostics);
        }

        private static async Task<Document> removeCatchAsync(
            Document document,
            TryStatementSyntax tryStatement,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            if (tryStatement.Finally is not null)
            {
                return document.WithSyntaxRoot(root.ReplaceNode(tryStatement, tryStatement.WithCatches([])));
            }

            if (tryStatement.Parent is BlockSyntax block)
            {
                SyntaxList<StatementSyntax> statements = tryStatement.Block.Statements;
                if (statements.Count > 0)
                {
                    statements = statements.Replace(
                        statements[0],
                        statements[0].WithLeadingTrivia(tryStatement.GetLeadingTrivia()));
                }

                int index = block.Statements.IndexOf(tryStatement);
                BlockSyntax updated = block.WithStatements(
                    block.Statements.RemoveAt(index).InsertRange(index, statements));
                return document.WithSyntaxRoot(root.ReplaceNode(block, updated));
            }

            return document.WithSyntaxRoot(root.ReplaceNode(tryStatement, tryStatement.Block));
        }
    }
}
