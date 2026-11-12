using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>
    /// Enumerates the operation roots that execute as part of a method (its body, constructor
    /// initializer and, for constructors, field and property initializers) and the throws within them.
    /// </summary>
    internal sealed class MemberOperationEnumerator
    {
        private readonly Compilation compilation;

        internal MemberOperationEnumerator(Compilation compilation)
        {
            this.compilation = compilation;
        }

        internal IEnumerable<IThrowOperation> EnumerateThrows(
            IMethodSymbol method,
            CancellationToken cancellationToken = default)
        {
            foreach (IOperation root in this.EnumerateRoots(method))
            {
                foreach (IThrowOperation throwOperation in enumerateThrows(root, cancellationToken))
                {
                    yield return throwOperation;
                }
            }
        }

        internal IEnumerable<IOperation> EnumerateRoots(IMethodSymbol method)
        {
            foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
            {
                foreach (IOperation operation in this.operationsForDeclaration(reference.GetSyntax()))
                {
                    yield return operation;
                }
            }

            if (method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor) ||
                !ExecutableHostResolver.RunsInitializers(method))
            {
                yield break;
            }

            foreach (ISymbol member in method.ContainingType.GetMembers())
            {
                if (member.IsImplicitlyDeclared || member.IsStatic != method.IsStatic)
                {
                    continue;
                }

                if (member is not (IFieldSymbol or IPropertySymbol) || member is IFieldSymbol { IsConst: true })
                {
                    continue;
                }

                foreach (SyntaxReference reference in member.DeclaringSyntaxReferences)
                {
                    SyntaxNode? value = getInitializerValue(reference.GetSyntax());
                    if (value is null)
                    {
                        continue;
                    }

                    SemanticModel model = this.compilation.GetSemanticModel(value.SyntaxTree);
                    if (model.GetOperation(value) is IOperation initializerOperation)
                    {
                        yield return initializerOperation;
                    }
                }
            }
        }

        private IEnumerable<IOperation> operationsForDeclaration(SyntaxNode syntax)
        {
            SemanticModel model = this.compilation.GetSemanticModel(syntax.SyntaxTree);
            if (model.GetOperation(syntax) is IOperation operation)
            {
                yield return operation;
                yield break;
            }

            switch (syntax)
            {
                case BaseMethodDeclarationSyntax declaration:
                    if (declaration.Body is { } body && model.GetOperation(body) is IOperation bodyOperation)
                    {
                        yield return bodyOperation;
                    }

                    if (declaration.ExpressionBody is { } expressionBody &&
                        model.GetOperation(expressionBody) is IOperation expressionOperation)
                    {
                        yield return expressionOperation;
                    }

                    if (declaration is ConstructorDeclarationSyntax { Initializer: { } initializer } &&
                        model.GetOperation(initializer) is IOperation initializerOperation)
                    {
                        yield return initializerOperation;
                    }

                    break;
                case AccessorDeclarationSyntax accessor:
                    if (accessor.Body is { } accessorBody &&
                        model.GetOperation(accessorBody) is IOperation accessorBodyOperation)
                    {
                        yield return accessorBodyOperation;
                    }

                    if (accessor.ExpressionBody is { } accessorExpression &&
                        model.GetOperation(accessorExpression) is IOperation accessorExpressionOperation)
                    {
                        yield return accessorExpressionOperation;
                    }

                    break;
            }
        }

        private static IEnumerable<IThrowOperation> enumerateThrows(IOperation operation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (OperationScope.IsIgnored(operation))
            {
                yield break;
            }

            if (operation is IThrowOperation throwOperation)
            {
                yield return throwOperation;
            }

            foreach (IOperation child in operation.ChildOperations)
            {
                foreach (IThrowOperation nested in enumerateThrows(child, cancellationToken))
                {
                    yield return nested;
                }
            }
        }

        private static SyntaxNode? getInitializerValue(SyntaxNode syntax)
        {
            return syntax switch
            {
                VariableDeclaratorSyntax { Initializer: { } initializer } => initializer.Value,
                PropertyDeclarationSyntax { Initializer: { } initializer } => initializer.Value,
                _ => null,
            };
        }
    }
}