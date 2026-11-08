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
                !compilationContext.Reachability.IsReachable(tryOperation))
            {
                return;
            }

            var inspector = new TryBodyInspector(compilationContext);
            if (!inspector.TryGetDoesNotThrowTargets(tryOperation.Body, out ImmutableArray<string> names))
            {
                return;
            }

            Location location = tryOperation.Syntax is TryStatementSyntax trySyntax
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

            string[] quoted = names.Select(name => $"'{name}'").ToArray();
            string list = quoted.Length == 2
                ? $"{quoted[0]} and {quoted[1]}"
                : $"{string.Join(", ", quoted.Take(quoted.Length - 1))}, and {quoted[quoted.Length - 1]}";
            return $"{list} have [DoesNotThrow] contracts";
        }

        private sealed class TryBodyInspector
        {
            private readonly ExceptionableCompilationContext compilationContext;
            private readonly List<string> names = [];

            internal TryBodyInspector(ExceptionableCompilationContext compilationContext)
            {
                this.compilationContext = compilationContext;
            }

            internal bool TryGetDoesNotThrowTargets(IOperation body, out ImmutableArray<string> targets)
            {
                if (this.walk(body) == Step.Disqualified || this.names.Count == 0)
                {
                    targets = [];
                    return false;
                }

                targets = [.. this.names];
                return true;
            }

            private Step walk(IOperation operation)
            {
                if (!this.compilationContext.Reachability.IsReachable(operation))
                {
                    return Step.Ok;
                }

                // Keep this set aligned with the non-throwing operations in DESIGN.md section 6.
                return operation switch
                {
                    IAnonymousFunctionOperation or ILocalFunctionOperation or INameOfOperation => Step.Ok,
                    IThrowOperation => Step.Disqualified,
                    IInvocationOperation or IObjectCreationOperation or IPropertyReferenceOperation or IEventAssignmentOperation => this.walkCall(operation),
                    IBinaryOperation binary => this.walkBinary(binary),
                    IUnaryOperation unary => this.walkUnary(unary),
                    IIncrementOrDecrementOperation increment when increment.OperatorMethod is not null => this.walkCall(increment),
                    IConversionOperation conversion when conversion.OperatorMethod is not null => this.walkCall(conversion),
                    IConversionOperation conversion when !isNonThrowingConversion(conversion) => Step.Disqualified,
                    IConditionalOperation or ICoalesceOperation or IConditionalAccessOperation => this.walkChildren(operation),
                    IBlockOperation or IExpressionStatementOperation or IReturnOperation or IArgumentOperation or IOmittedArgumentOperation or IVariableDeclarationGroupOperation or IVariableDeclarationOperation or IVariableDeclaratorOperation or IVariableInitializerOperation or ISimpleAssignmentOperation or IObjectOrCollectionInitializerOperation or IEventReferenceOperation or IMethodReferenceOperation or IConditionalAccessInstanceOperation or IInstanceReferenceOperation or ILocalReferenceOperation or IParameterReferenceOperation or ILiteralOperation or IDefaultValueOperation or IDiscardOperation or ITypeOfOperation or IEmptyOperation or IConversionOperation => this.walkChildren(operation),
                    _ => Step.Disqualified,
                };
            }

            private Step walkBinary(IBinaryOperation binary)
            {
                if (binary.OperatorMethod is not null)
                {
                    return this.walkCall(binary);
                }

                if (binary.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr)
                {
                    return this.walkChildren(binary);
                }

                return Step.Disqualified;
            }

            private Step walkUnary(IUnaryOperation unary)
            {
                if (unary.OperatorMethod is not null)
                {
                    return this.walkCall(unary);
                }

                if (unary.OperatorKind == UnaryOperatorKind.Not)
                {
                    return this.walkChildren(unary);
                }

                return Step.Disqualified;
            }

            private Step walkCall(IOperation operation)
            {
                IMethodSymbol[] targets = [.. OperationTargetResolver.GetTargetMethods(operation)];
                if (targets.Length == 0 || targets.Any(target => !this.isProvenDoesNotThrow(target, operation)))
                {
                    return Step.Disqualified;
                }

                if (this.walkChildren(operation) == Step.Disqualified)
                {
                    return Step.Disqualified;
                }

                foreach (IMethodSymbol? target in targets)
                {
                    string displayName = target.GetContractDisplayName();
                    if (!this.names.Contains(displayName))
                    {
                        this.names.Add(displayName);
                    }
                }

                return Step.Ok;
            }

            private Step walkChildren(IOperation operation)
            {
                foreach (IOperation child in operation.ChildOperations)
                {
                    if (this.walk(child) == Step.Disqualified)
                    {
                        return Step.Disqualified;
                    }
                }

                return Step.Ok;
            }

            private bool isProvenDoesNotThrow(IMethodSymbol target, IOperation operation)
            {
                if (!this.compilationContext.IsSupportedMethod(target) ||
                    MethodAnalysisPolicy.IsDeferredReturnType(target.ReturnType) ||
                    !MethodAnalysisPolicy.HasCertainDispatch(target, operation))
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