using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class UnnecessaryCatchAnalysis
    {
        internal static void Analyze(
            OperationAnalysisContext operationContext,
            ExceptionableCompilationContext compilationContext)
        {
            if (operationContext.Operation is not ITryOperation tryOperation ||
                tryOperation.Catches.IsEmpty ||
                !compilationContext.Options.IsEnabled(tryOperation.Syntax.SyntaxTree) ||
                operationContext.ContainingSymbol is not IMethodSymbol callerMethod ||
                !compilationContext.IsSupportedMethod(callerMethod) ||
                !compilationContext.Reachability.IsReachable(tryOperation, callerMethod))
            {
                return;
            }

            var inspector = new TryBodyInspector(compilationContext, callerMethod);
            if (!inspector.TryGetDoesNotThrowTargets(tryOperation.Body, out var names))
            {
                return;
            }

            var location = tryOperation.Syntax is TryStatementSyntax trySyntax
                ? trySyntax.TryKeyword.GetLocation()
                : tryOperation.Syntax.GetLocation();
            operationContext.ReportDiagnostic(Diagnostic.Create(
                ExceptionableDiagnostics.UnnecessaryTryCatch,
                location,
                describeTargets(names)));
        }

        private static bool isNonThrowingConversion(IConversionOperation conversion)
        {
            if (conversion.Conversion.IsUserDefined)
            {
                return false;
            }

            return conversion.IsImplicit ||
                (conversion.Syntax is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AsExpression));
        }

        private static string describeTargets(ImmutableArray<string> names)
        {
            if (names.Length == 1)
            {
                return $"'{names[0]}' has a [DoesNotThrow] contract";
            }

            var quoted = names.Select(name => $"'{name}'").ToArray();
            var list = quoted.Length == 2
                ? $"{quoted[0]} and {quoted[1]}"
                : $"{string.Join(", ", quoted.Take(quoted.Length - 1))}, and {quoted[quoted.Length - 1]}";
            return $"{list} have [DoesNotThrow] contracts";
        }

        private sealed class TryBodyInspector
        {
            private readonly ExceptionableCompilationContext compilationContext;
            private readonly IMethodSymbol method;
            private readonly List<string> names = new();

            internal TryBodyInspector(ExceptionableCompilationContext compilationContext, IMethodSymbol method)
            {
                this.compilationContext = compilationContext;
                this.method = method;
            }

            internal bool TryGetDoesNotThrowTargets(IOperation body, out ImmutableArray<string> targets)
            {
                if (this.walk(body) == Step.Disqualified || this.names.Count == 0)
                {
                    targets = ImmutableArray<string>.Empty;
                    return false;
                }

                targets = this.names.ToImmutableArray();
                return true;
            }

            private Step walk(IOperation operation)
            {
                if (!this.compilationContext.Reachability.IsReachable(operation, this.method))
                {
                    return Step.Ok;
                }

                switch (operation)
                {
                    case IAnonymousFunctionOperation:
                    case ILocalFunctionOperation:
                        return Step.Ok;
                    case IThrowOperation:
                        return Step.Disqualified;
                    case IInvocationOperation:
                    case IObjectCreationOperation:
                    case IPropertyReferenceOperation:
                    case IEventAssignmentOperation:
                        return this.walkCall(operation);
                    case IConversionOperation conversion when !isNonThrowingConversion(conversion):
                        return Step.Disqualified;
                    case IBlockOperation:
                    case IExpressionStatementOperation:
                    case IReturnOperation:
                    case IArgumentOperation:
                    case IOmittedArgumentOperation:
                    case IVariableDeclarationGroupOperation:
                    case IVariableDeclarationOperation:
                    case IVariableDeclaratorOperation:
                    case IVariableInitializerOperation:
                    case ISimpleAssignmentOperation:
                    case IObjectOrCollectionInitializerOperation:
                    case IEventReferenceOperation:
                    case IMethodReferenceOperation:
                    case IInstanceReferenceOperation:
                    case ILocalReferenceOperation:
                    case IParameterReferenceOperation:
                    case ILiteralOperation:
                    case IDefaultValueOperation:
                    case IDiscardOperation:
                    case INameOfOperation:
                    case ITypeOfOperation:
                    case IEmptyOperation:
                    case IConversionOperation:
                        return this.walkChildren(operation);
                    default:
                        return Step.Disqualified;
                }
            }

            private Step walkCall(IOperation operation)
            {
                var targets = OperationTargetResolver.GetTargetMethods(operation).ToArray();
                if (targets.Length == 0 || targets.Any(target => !this.isProvenDoesNotThrow(target)))
                {
                    return Step.Disqualified;
                }

                if (this.walkChildren(operation) == Step.Disqualified)
                {
                    return Step.Disqualified;
                }

                foreach (var target in targets)
                {
                    var displayName = target.GetContractDisplayName();
                    if (!this.names.Contains(displayName))
                    {
                        this.names.Add(displayName);
                    }
                }

                return Step.Ok;
            }

            private Step walkChildren(IOperation operation)
            {
                foreach (var child in operation.ChildOperations)
                {
                    if (this.walk(child) == Step.Disqualified)
                    {
                        return Step.Disqualified;
                    }
                }

                return Step.Ok;
            }

            private bool isProvenDoesNotThrow(IMethodSymbol target)
            {
                if (!this.compilationContext.IsSupportedMethod(target) ||
                    MethodAnalysisPolicy.IsDeferredReturnType(target.ReturnType) ||
                    (MethodAnalysisPolicy.IsDispatchUncertain(target) &&
                        target.ContainingType.TypeKind != TypeKind.Interface))
                {
                    return false;
                }

                return this.compilationContext.ContractResolver.GetEffectiveContract(target).IsDoesNotThrow;
            }
        }

        private enum Step
        {
            Ok,
            Disqualified,
        }
    }
}