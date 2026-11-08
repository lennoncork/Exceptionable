using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class MethodAnalysisPolicy
    {
        internal static bool IsSupportedMethod(IMethodSymbol method)
        {
            if (method.IsAsync ||
                method.MethodKind == MethodKind.AnonymousFunction ||
                method.MethodKind == MethodKind.LocalFunction ||
                method.MethodKind == MethodKind.UserDefinedOperator ||
                method.MethodKind == MethodKind.Conversion)
            {
                return false;
            }

            return !method.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .Any(syntax => syntax.DescendantNodesAndSelf().OfType<YieldStatementSyntax>().Any());
        }

        internal static bool IsDeferredReturnType(ITypeSymbol returnType)
        {
            if (returnType is not INamedTypeSymbol namedType)
            {
                return false;
            }

            var namespaceName = namedType.ContainingNamespace.ToDisplayString();
            return (namespaceName == "System.Threading.Tasks" &&
                    (namedType.Name == "Task" || namedType.Name == "ValueTask")) ||
                (namespaceName == "System.Collections" &&
                    (namedType.Name == "IEnumerable" || namedType.Name == "IEnumerator")) ||
                (namespaceName == "System.Collections.Generic" &&
                    (namedType.Name == "IEnumerable" || namedType.Name == "IEnumerator" ||
                        namedType.Name == "IAsyncEnumerable" || namedType.Name == "IAsyncEnumerator"));
        }

        internal static bool IsDispatchUncertain(IMethodSymbol method) =>
            method.IsVirtual || method.IsAbstract || method.IsOverride;
    }
}