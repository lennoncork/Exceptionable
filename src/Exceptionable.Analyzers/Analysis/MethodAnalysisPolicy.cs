using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class MethodAnalysisPolicy
    {
        internal static bool IsSupportedMethod(IMethodSymbol method)
        {
            if (method.IsAsync || method.MethodKind == MethodKind.AnonymousFunction)
            {
                return false;
            }

            foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
            {
                SyntaxNode syntax = reference.GetSyntax();
                foreach (YieldStatementSyntax yieldStatement in syntax.DescendantNodes().OfType<YieldStatementSyntax>())
                {
                    if (!isNestedFunctionYield(yieldStatement, syntax))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool HasExecutableBody(IMethodSymbol method)
        {
            foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
            {
                switch (reference.GetSyntax())
                {
                    case BaseMethodDeclarationSyntax declaration
                        when declaration.Body is not null || declaration.ExpressionBody is not null:
                    case LocalFunctionStatementSyntax local
                        when local.Body is not null || local.ExpressionBody is not null:
                    case AccessorDeclarationSyntax accessor
                        when accessor.Body is not null || accessor.ExpressionBody is not null:
                        return true;
                }
            }

            return false;
        }

        internal static bool IsDeferredReturnType(ITypeSymbol returnType)
        {
            if (returnType is not INamedTypeSymbol namedType)
            {
                return false;
            }

            string namespaceName = namedType.ContainingNamespace.ToDisplayString();
            return (namespaceName == "System.Threading.Tasks" &&
                    (namedType.Name == "Task" || namedType.Name == "ValueTask")) ||
                (namespaceName == "System.Collections" &&
                    (namedType.Name == "IEnumerable" || namedType.Name == "IEnumerator")) ||
                (namespaceName == "System.Collections.Generic" &&
                    (namedType.Name == "IEnumerable" || namedType.Name == "IEnumerator" ||
                        namedType.Name == "IAsyncEnumerable" || namedType.Name == "IAsyncEnumerator"));
        }

        internal static bool IsDispatchUncertain(IMethodSymbol method)
        {
            if (method.IsAbstract)
            {
                return true;
            }

            if (!method.IsVirtual && !method.IsOverride)
            {
                return false;
            }

            return !method.IsSealed && !method.ContainingType.IsSealed;
        }

        internal static bool HasCertainDispatch(IMethodSymbol method, IOperation operation)
        {
            if (IsBaseAccess(operation))
            {
                return true;
            }

            return !IsDispatchUncertain(method) || method.ContainingType.TypeKind == TypeKind.Interface;
        }

        internal static bool IsBaseAccess(IOperation operation)
        {
            IOperation? instance = operation switch
            {
                IInvocationOperation invocation => invocation.Instance,
                IMemberReferenceOperation member => member.Instance,
                IEventAssignmentOperation eventAssignment =>
                    (eventAssignment.EventReference as IMemberReferenceOperation)?.Instance,
                _ => null,
            };
            return instance?.Syntax is BaseExpressionSyntax;
        }

        internal static bool IsGenerated(IMethodSymbol method)
        {
            if (method.IsImplicitlyDeclared)
            {
                return true;
            }

            foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
            {
                if (!isGeneratedTree(reference.SyntaxTree))
                {
                    return false;
                }
            }

            return method.DeclaringSyntaxReferences.Length > 0;
        }

        private static bool isGeneratedTree(SyntaxTree tree)
        {
            string path = tree.FilePath ?? string.Empty;
            if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Microsoft.CodeAnalysis.Text.SourceText text = tree.GetText();
            int length = Math.Min(text.Length, 240);
            return length > 0 &&
                text.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(0, length)).Contains("<auto-generated");
        }

        private static bool isNestedFunctionYield(SyntaxNode yieldStatement, SyntaxNode methodSyntax)
        {
            for (SyntaxNode? current = yieldStatement.Parent;
                current is not null && !ReferenceEquals(current, methodSyntax);
                current = current.Parent)
            {
                if (current is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
                {
                    return true;
                }
            }

            return false;
        }
    }
}