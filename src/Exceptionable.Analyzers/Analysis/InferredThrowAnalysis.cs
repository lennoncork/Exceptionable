using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class InferredThrowAnalysis
    {
        private readonly Compilation compilation;
        private readonly ReachabilityAnalysis reachability;
        private readonly ExceptionTypeResolver exceptionTypes;
        private readonly INamedTypeSymbol? exceptionBaseType;
        private readonly INamedTypeSymbol? argumentNullException;
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<INamedTypeSymbol>> implicitTypes =
            new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<IAssemblySymbol, ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>>> recorded =
            new(SymbolEqualityComparer.Default);

        internal InferredThrowAnalysis(Compilation compilation, ReachabilityAnalysis reachability)
        {
            this.compilation = compilation;
            this.reachability = reachability;
            this.exceptionTypes = new ExceptionTypeResolver(compilation);
            this.exceptionBaseType = compilation.GetTypeByMetadataName("System.Exception");
            this.argumentNullException = compilation.GetTypeByMetadataName("System.ArgumentNullException");
        }

        internal ImmutableArray<INamedTypeSymbol> GetImplicitDeclaredTypes(IMethodSymbol method)
        {
            if (method.DeclaringSyntaxReferences.Length == 0)
            {
                return this.readRecordedThrows(method);
            }

            return this.implicitTypes.GetOrAdd(method, this.scanImplicitTypes);
        }

        internal bool IsRedundantNullableGuard(IThrowOperation throwOperation, IReadOnlyCollection<IMethodSymbol> hosts)
        {
            if (hosts.Count == 0)
            {
                return false;
            }

            foreach (IMethodSymbol host in hosts)
            {
                if (this.classify(throwOperation, host).Kind != GuardKind.RedundantNullable)
                {
                    return false;
                }
            }

            return true;
        }

        private ImmutableArray<INamedTypeSymbol> scanImplicitTypes(IMethodSymbol method)
        {
            if (!MethodAnalysisPolicy.IsSupportedMethod(method) || this.isGenerated(method))
            {
                return [];
            }

            var types = new List<INamedTypeSymbol>();
            foreach (IThrowOperation throwOperation in this.enumerateMemberThrows(method))
            {
                ClassifiedThrow classified = this.classify(throwOperation, method);
                if (classified.Kind != GuardKind.ImplicitContract || classified.ExceptionType is null)
                {
                    continue;
                }

                if (types.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, classified.ExceptionType)))
                {
                    continue;
                }

                types.Add(classified.ExceptionType);
            }

            types.Sort(static (left, right) => string.CompareOrdinal(
                left.GetExceptionDisplayName(),
                right.GetExceptionDisplayName()));
            return types.ToImmutableArray();
        }

        private ClassifiedThrow classify(IThrowOperation throwOperation, IMethodSymbol method)
        {
            if (this.exceptionBaseType is null ||
                OperationScope.IsIgnored(throwOperation) ||
                !this.reachability.IsReachable(throwOperation))
            {
                return ClassifiedThrow.None;
            }

            ITypeSymbol? exceptionType = throwOperation.Exception is null
                ? null
                : ExceptionFlowAnalysis.GetThrownExceptionType(throwOperation.Exception, this.compilation);
            exceptionType ??= ExceptionFlowAnalysis.GetRethrownExceptionType(throwOperation, this.compilation);
            if (exceptionType is not INamedTypeSymbol named ||
                named.TypeKind == TypeKind.Error ||
                !named.InheritsOrEquals(this.exceptionBaseType) ||
                ExceptionFlowAnalysis.IsCaught(throwOperation, named))
            {
                return ClassifiedThrow.None;
            }

            // Nullable annotations are not a runtime check. EXC007 still treats a
            // non-nullable reference parameter as a promise that the caller will not pass null.
            return this.isRedundantGuard(throwOperation, method, named)
                ? new ClassifiedThrow(GuardKind.RedundantNullable, named)
                : new ClassifiedThrow(GuardKind.ImplicitContract, named);
        }

        internal ImmutableArray<INamedTypeSymbol> GetObservedEscapingTypes(
            IMethodSymbol method,
            ContractResolver contracts)
        {
            if (!MethodAnalysisPolicy.IsSupportedMethod(method) || this.isGenerated(method))
            {
                return [];
            }

            var types = new List<INamedTypeSymbol>();
            foreach (IThrowOperation throwOperation in this.enumerateMemberThrows(method))
            {
                ClassifiedThrow classified = this.classify(throwOperation, method);
                if (classified.ExceptionType is null ||
                    classified.Kind is not (GuardKind.ImplicitContract or GuardKind.RedundantNullable))
                {
                    continue;
                }

                addObserved(types, classified.ExceptionType);
            }

            foreach (IOperation root in this.enumerateRoots(method))
            {
                this.collectCallExceptions(root, method, contracts, types);
            }

            return types.ToImmutableArray();
        }

        private void collectCallExceptions(
            IOperation operation,
            IMethodSymbol caller,
            ContractResolver contracts,
            List<INamedTypeSymbol> types)
        {
            if (operation is ILocalFunctionOperation or IAnonymousFunctionOperation or INameOfOperation)
            {
                return;
            }

            if (!OperationScope.IsIgnored(operation) && this.reachability.IsReachable(operation))
            {
                collectTargets(operation, caller, contracts, types, OperationTargetResolver.GetTargetMethods(operation), false);
                collectTargets(operation, caller, contracts, types, OperationTargetResolver.GetPatternMethods(operation), true);
            }

            foreach (IOperation child in operation.ChildOperations)
            {
                this.collectCallExceptions(child, caller, contracts, types);
            }
        }

        private static void collectTargets(
            IOperation operation,
            IMethodSymbol caller,
            ContractResolver contracts,
            List<INamedTypeSymbol> types,
            IEnumerable<IMethodSymbol> targets,
            bool allowDeferredReturn)
        {
            foreach (IMethodSymbol target in targets)
            {
                if (target.IsSameMethod(caller) || !isAnalyzableTarget(target, operation, allowDeferredReturn))
                {
                    continue;
                }

                foreach (INamedTypeSymbol declaredType in contracts.GetEffectiveContract(target).DeclaredTypes)
                {
                    if (!ExceptionFlowAnalysis.IsCaught(operation, declaredType))
                    {
                        addObserved(types, declaredType);
                    }
                }
            }
        }

        private static bool isAnalyzableTarget(
            IMethodSymbol target,
            IOperation operation,
            bool allowDeferredReturn)
        {
            if (!MethodAnalysisPolicy.HasCertainDispatch(target, operation) ||
                !MethodAnalysisPolicy.IsSupportedMethod(target))
            {
                return false;
            }

            return allowDeferredReturn || !MethodAnalysisPolicy.IsDeferredReturnType(target.ReturnType);
        }

        private static void addObserved(List<INamedTypeSymbol> types, INamedTypeSymbol exceptionType)
        {
            if (!types.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, exceptionType)))
            {
                types.Add(exceptionType);
            }
        }

        private bool isRedundantGuard(
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

        private ImmutableArray<INamedTypeSymbol> readRecordedThrows(IMethodSymbol method)
        {
            string? memberId = method.GetDocumentationCommentId();
            if (memberId is null)
            {
                return [];
            }

            ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>> map =
                this.recorded.GetOrAdd(method.ContainingAssembly, this.readAssembly);
            return map.TryGetValue(memberId, out ImmutableArray<INamedTypeSymbol> types)
                ? types
                : [];
        }

        private ImmutableDictionary<string, ImmutableArray<INamedTypeSymbol>> readAssembly(IAssemblySymbol assembly)
        {
            var builder = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
            foreach (AttributeData attribute in assembly.GetAttributes())
            {
                if (!InferredThrowsMetadata.IsInferredThrows(attribute) ||
                    attribute.ConstructorArguments.Length < 2 ||
                    attribute.ConstructorArguments[0].Value is not string memberId ||
                    attribute.ConstructorArguments[1].Value is not string exceptionName)
                {
                    continue;
                }

                INamedTypeSymbol? exceptionType = this.exceptionTypes.ResolveExceptionType(exceptionName);
                if (exceptionType is null)
                {
                    continue;
                }

                if (!builder.TryGetValue(memberId, out List<INamedTypeSymbol>? types))
                {
                    types = [];
                    builder.Add(memberId, types);
                }

                if (!types.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, exceptionType)))
                {
                    types.Add(exceptionType);
                }
            }

            var result = ImmutableDictionary.CreateBuilder<string, ImmutableArray<INamedTypeSymbol>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<INamedTypeSymbol>> entry in builder)
            {
                result.Add(entry.Key, entry.Value.ToImmutableArray());
            }

            return result.ToImmutable();
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

        private bool isGenerated(IMethodSymbol method)
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

        private IEnumerable<IThrowOperation> enumerateMemberThrows(IMethodSymbol method)
        {
            foreach (IOperation root in this.enumerateRoots(method))
            {
                foreach (IThrowOperation throwOperation in enumerateThrows(root))
                {
                    yield return throwOperation;
                }
            }
        }

        private IEnumerable<IOperation> enumerateRoots(IMethodSymbol method)
        {
            foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
            {
                foreach (IOperation operation in this.operationsForDeclaration(reference.GetSyntax()))
                {
                    yield return operation;
                }
            }

            if (method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor) ||
                !ConstructorHost.RunsInitializers(method))
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

        private readonly struct ClassifiedThrow
        {
            internal static ClassifiedThrow None { get; } = new(GuardKind.None, null);

            internal ClassifiedThrow(GuardKind kind, INamedTypeSymbol? exceptionType)
            {
                this.Kind = kind;
                this.ExceptionType = exceptionType;
            }

            internal GuardKind Kind { get; }

            internal INamedTypeSymbol? ExceptionType { get; }
        }
    }
}