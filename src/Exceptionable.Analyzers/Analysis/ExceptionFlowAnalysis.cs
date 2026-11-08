using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class ExceptionFlowAnalysis
    {
        internal static bool IsCaught(IOperation operation, ITypeSymbol exceptionType)
        {
            for (var current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current is not ITryOperation tryOperation ||
                    tryOperation.Catches.Any(catchClause => isDescendantOf(operation, catchClause.Handler)) ||
                    !isDescendantOf(operation, tryOperation.Body))
                {
                    continue;
                }

                foreach (var catchClause in tryOperation.Catches)
                {
                    if (!isFilterDefinitelyTrue(catchClause.Filter) ||
                        (catchClause.ExceptionType is not null &&
                            !exceptionType.InheritsOrEquals(catchClause.ExceptionType)))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        internal static ITypeSymbol? GetRethrownExceptionType(
            IThrowOperation throwOperation,
            Compilation compilation)
        {
            for (var current = throwOperation.Parent; current is not null; current = current.Parent)
            {
                if (current is ICatchClauseOperation catchClause &&
                    isDescendantOf(throwOperation, catchClause.Handler))
                {
                    if (catchClause.Syntax is CatchClauseSyntax rethrowCatchSyntax &&
                        rethrowCatchSyntax.Declaration is null)
                    {
                        return compilation.GetTypeByMetadataName("System.Exception");
                    }

                    return catchClause.ExceptionType ?? compilation.GetTypeByMetadataName("System.Exception");
                }
            }

            var catchClauseSyntax = throwOperation.Syntax.AncestorsAndSelf()
                .OfType<CatchClauseSyntax>()
                .FirstOrDefault();
            if (catchClauseSyntax is null ||
                !catchClauseSyntax.Block.Span.Contains(throwOperation.Syntax.Span))
            {
                return null;
            }

            return catchClauseSyntax.Declaration is null
                ? compilation.GetTypeByMetadataName("System.Exception")
                : null;
        }

        internal static ITypeSymbol? GetThrownExceptionType(IOperation exception)
        {
            while (exception is IConversionOperation conversion && conversion.IsImplicit)
            {
                exception = conversion.Operand;
            }

            return exception.Type;
        }

        private static bool isDescendantOf(IOperation operation, IOperation ancestor)
        {
            for (var current = operation; current is not null; current = current.Parent)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool isFilterDefinitelyTrue(IOperation? filter)
        {
            if (filter is null)
            {
                return true;
            }

            var constantValue = filter.ConstantValue;
            return constantValue.HasValue && constantValue.Value is true;
        }
    }
}