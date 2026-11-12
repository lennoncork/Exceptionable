using System.Collections.Immutable;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>
    /// Recognizes <see cref="System.ArgumentNullException"/> guards that nullable annotations
    /// already make redundant in a constructor.
    /// </summary>
    internal sealed class NullableGuardDetector
    {
        private readonly Compilation compilation;
        private readonly INamedTypeSymbol? argumentNullException;

        internal NullableGuardDetector(Compilation compilation)
        {
            this.compilation = compilation;
            this.argumentNullException = compilation.GetTypeByMetadataName("System.ArgumentNullException");
        }

        internal bool IsRedundantGuard(
            IThrowOperation throwOperation,
            IMethodSymbol constructor,
            INamedTypeSymbol exceptionType)
        {
            if (this.argumentNullException is null ||
                constructor.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor) ||
                !exceptionType.InheritsOrEquals(this.argumentNullException) ||
                !this.annotationsEnabled(constructor))
            {
                return false;
            }

            IParameterSymbol? parameter = tryGetGuardedParameter(throwOperation, constructor);
            if (parameter is not null)
            {
                return isNonNullableReference(parameter);
            }

            // An argument that does not name one of this constructor's parameters is not
            // evidence that every reference parameter is non-nullable.
            if (hasExceptionArgument(throwOperation))
            {
                return false;
            }

            bool sawReferenceParameter = false;
            foreach (IParameterSymbol candidate in constructor.Parameters)
            {
                if (candidate.Type.IsReferenceType != true)
                {
                    continue;
                }

                sawReferenceParameter = true;
                if (!isNonNullableReference(candidate))
                {
                    return false;
                }
            }

            return sawReferenceParameter;
        }

        private bool annotationsEnabled(IMethodSymbol constructor)
        {
            if (this.anyAnnotationsEnabled(constructor.DeclaringSyntaxReferences))
            {
                return true;
            }

            return constructor.DeclaringSyntaxReferences.Length == 0 &&
                this.anyAnnotationsEnabled(constructor.ContainingType.DeclaringSyntaxReferences);
        }

        private bool anyAnnotationsEnabled(ImmutableArray<SyntaxReference> references)
        {
            foreach (SyntaxReference reference in references)
            {
                SyntaxNode syntax = reference.GetSyntax();
                SemanticModel model = this.compilation.GetSemanticModel(syntax.SyntaxTree);
                NullableContext context = model.GetNullableContext(syntax.SpanStart);
                if ((context & NullableContext.AnnotationsEnabled) == NullableContext.AnnotationsEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static IParameterSymbol? tryGetGuardedParameter(
            IThrowOperation throwOperation,
            IMethodSymbol constructor)
        {
            IOperation? creation = unwrap(throwOperation.Exception);
            if (creation is not IObjectCreationOperation objectCreation || objectCreation.Arguments.Length == 0)
            {
                return null;
            }

            IOperation argument = unwrap(objectCreation.Arguments[0].Value)!;
            if (argument is INameOfOperation nameofOperation)
            {
                argument = unwrap(nameofOperation.Argument)!;
            }

            if (argument is IParameterReferenceOperation parameterReference &&
                SymbolEqualityComparer.Default.Equals(parameterReference.Parameter.ContainingSymbol, constructor))
            {
                return parameterReference.Parameter;
            }

            if (argument.ConstantValue.HasValue && argument.ConstantValue.Value is string name)
            {
                foreach (IParameterSymbol candidate in constructor.Parameters)
                {
                    if (candidate.Name == name)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static bool hasExceptionArgument(IThrowOperation throwOperation)
        {
            IOperation? creation = unwrap(throwOperation.Exception);
            return creation is IObjectCreationOperation { Arguments.Length: > 0 };
        }

        private static bool isNonNullableReference(IParameterSymbol parameter)
        {
            if (parameter.Type.IsReferenceType != true ||
                parameter.NullableAnnotation != NullableAnnotation.NotAnnotated ||
                allowsNull(parameter))
            {
                return false;
            }

            return true;
        }

        private static bool allowsNull(IParameterSymbol parameter)
        {
            foreach (AttributeData attribute in parameter.GetAttributes())
            {
                INamedTypeSymbol? type = attribute.AttributeClass;
                if (type?.Name == "AllowNullAttribute" &&
                    type.ContainingNamespace?.ToDisplayString() == "System.Diagnostics.CodeAnalysis")
                {
                    return true;
                }
            }

            return false;
        }

        private static IOperation? unwrap(IOperation? operation)
        {
            while (operation is IConversionOperation conversion && conversion.IsImplicit)
            {
                operation = conversion.Operand;
            }

            return operation;
        }
    }
}