using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class ArgumentNullConstructorAnalysis
    {
        private readonly Compilation compilation;
        private readonly ReachabilityAnalysis reachability;
        private readonly INamedTypeSymbol? argumentNullException;
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<INamedTypeSymbol>> implicitTypes =
            new(SymbolEqualityComparer.Default);

        internal ArgumentNullConstructorAnalysis(Compilation compilation, ReachabilityAnalysis reachability)
        {
            this.compilation = compilation;
            this.reachability = reachability;
            this.argumentNullException = compilation.GetTypeByMetadataName("System.ArgumentNullException");
        }

        internal ImmutableArray<INamedTypeSymbol> GetImplicitDeclaredTypes(IMethodSymbol method) =>
            this.implicitTypes.GetOrAdd(method, this.scanImplicitTypes);

        internal bool IsRedundantNullableGuard(IThrowOperation throwOperation, IReadOnlyCollection<IMethodSymbol> hosts)
        {
            if (hosts.Count == 0)
            {
                return false;
            }

            foreach (IMethodSymbol host in hosts)
            {
                if (this.classify(throwOperation, host) != GuardKind.RedundantNullable)
                {
                    return false;
                }
            }

            return true;
        }

        private ImmutableArray<INamedTypeSymbol> scanImplicitTypes(IMethodSymbol method)
        {
            if (this.argumentNullException is null ||
                method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor))
            {
                return [];
            }

            foreach (IThrowOperation throwOperation in this.enumerateConstructorThrows(method))
            {
                if (this.classify(throwOperation, method) == GuardKind.ImplicitContract)
                {
                    return [this.argumentNullException];
                }
            }

            return [];
        }

        private GuardKind classify(IThrowOperation throwOperation, IMethodSymbol method)
        {
            if (this.argumentNullException is null ||
                method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor) ||
                OperationScope.IsIgnored(throwOperation) ||
                !this.reachability.IsReachable(throwOperation))
            {
                return GuardKind.None;
            }

            ITypeSymbol? exceptionType = throwOperation.Exception is null
                ? null
                : ExceptionFlowAnalysis.GetThrownExceptionType(throwOperation.Exception, this.compilation);
            exceptionType ??= ExceptionFlowAnalysis.GetRethrownExceptionType(throwOperation, this.compilation);
            if (exceptionType is null ||
                !exceptionType.InheritsOrEquals(this.argumentNullException) ||
                ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType))
            {
                return GuardKind.None;
            }

            // Nullable annotations are not a runtime check. EXC007 still treats a
            // non-nullable reference parameter as a promise that the caller will not pass null.
            return this.isRedundantGuard(throwOperation, method)
                ? GuardKind.RedundantNullable
                : GuardKind.ImplicitContract;
        }

        private bool isRedundantGuard(IThrowOperation throwOperation, IMethodSymbol constructor)
        {
            if (!this.annotationsEnabled(constructor))
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

        private IEnumerable<IThrowOperation> enumerateConstructorThrows(IMethodSymbol constructor)
        {
            foreach (IOperation root in this.enumerateRoots(constructor))
            {
                foreach (IThrowOperation throwOperation in enumerateThrows(root))
                {
                    yield return throwOperation;
                }
            }
        }

        private IEnumerable<IOperation> enumerateRoots(IMethodSymbol constructor)
        {
            foreach (SyntaxReference reference in constructor.DeclaringSyntaxReferences)
            {
                SyntaxNode syntax = reference.GetSyntax();
                if (syntax is not ConstructorDeclarationSyntax constructorSyntax)
                {
                    continue;
                }

                SemanticModel model = this.compilation.GetSemanticModel(syntax.SyntaxTree);
                IOperation? operation = model.GetOperation(constructorSyntax);
                if (operation is not null)
                {
                    yield return operation;
                    continue;
                }

                if (constructorSyntax.Body is { } body &&
                    model.GetOperation(body) is IOperation bodyOperation)
                {
                    yield return bodyOperation;
                }

                if (constructorSyntax.ExpressionBody is { } expressionBody &&
                    model.GetOperation(expressionBody) is IOperation expressionOperation)
                {
                    yield return expressionOperation;
                }

                if (constructorSyntax.Initializer is { } initializer &&
                    model.GetOperation(initializer) is IOperation initializerOperation)
                {
                    yield return initializerOperation;
                }
            }

            if (!ConstructorHost.RunsInitializers(constructor))
            {
                yield break;
            }

            foreach (ISymbol member in constructor.ContainingType.GetMembers())
            {
                if (member.IsImplicitlyDeclared || member.IsStatic != constructor.IsStatic)
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

        private static IEnumerable<IThrowOperation> enumerateThrows(IOperation operation)
        {
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
                foreach (IThrowOperation nested in enumerateThrows(child))
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

        private enum GuardKind
        {
            None,
            ImplicitContract,
            RedundantNullable,
        }
    }
}
