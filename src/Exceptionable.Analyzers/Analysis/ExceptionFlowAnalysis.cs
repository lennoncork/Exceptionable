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
            for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current is not ITryOperation tryOperation ||
                    tryOperation.Catches.Any(catchClause => isDescendantOf(operation, catchClause.Handler)) ||
                    !isDescendantOf(operation, tryOperation.Body))
                {
                    continue;
                }

                foreach (ICatchClauseOperation catchClause in tryOperation.Catches)
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
            for (IOperation? current = throwOperation.Parent; current is not null; current = current.Parent)
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

            CatchClauseSyntax? catchClauseSyntax = throwOperation.Syntax.AncestorsAndSelf()
                .OfType<CatchClauseSyntax>()
                .FirstOrDefault();
            if (catchClauseSyntax is null ||
                !catchClauseSyntax.Block.Span.Contains(throwOperation.Syntax.Span))
            {
                return null;
            }

            if (catchClauseSyntax.Declaration is null)
            {
                return compilation.GetTypeByMetadataName("System.Exception");
            }

            return throwOperation.SemanticModel?.GetTypeInfo(catchClauseSyntax.Declaration.Type).Type;
        }

        internal static ITypeSymbol? GetThrownExceptionType(IOperation exception, Compilation compilation)
        {
            if (isNullConstant(exception))
            {
                return compilation.GetTypeByMetadataName("System.NullReferenceException");
            }

            while (exception is IConversionOperation conversion && conversion.IsImplicit)
            {
                exception = conversion.Operand;
                if (isNullConstant(exception))
                {
                    return compilation.GetTypeByMetadataName("System.NullReferenceException");
                }
            }

            return exception.Type;
        }

        private static bool isNullConstant(IOperation operation)
        {
            Optional<object?> constantValue = operation.ConstantValue;
            return constantValue.HasValue && constantValue.Value is null;
        }

        private static bool isDescendantOf(IOperation operation, IOperation ancestor)
        {
            for (IOperation? current = operation; current is not null; current = current.Parent)
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

            Optional<object?> constantValue = filter.ConstantValue;
            return constantValue.HasValue && constantValue.Value is true;
        }
    }
}