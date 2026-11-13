using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class InferredThrowAnalysis : IImplicitContractSource
    {
        private readonly Compilation compilation;
        private readonly ReachabilityAnalysis reachability;
        private readonly Func<IMethodSymbol, bool> isSupportedMethod;
        private readonly INamedTypeSymbol? exceptionBaseType;
        private readonly NullableGuardDetector nullableGuards;
        private readonly MemberOperationEnumerator operations;
        private readonly RecordedThrowsReader recordedThrows;
        private readonly ConcurrentDictionary<IMethodSymbol, ImmutableArray<INamedTypeSymbol>> implicitTypes =
            new(SymbolEqualityComparer.Default);

        internal InferredThrowAnalysis(
            Compilation compilation,
            ReachabilityAnalysis reachability,
            ExceptionTypeResolver exceptionTypes,
            Func<IMethodSymbol, bool> isSupportedMethod)
        {
            this.compilation = compilation;
            this.reachability = reachability;
            this.isSupportedMethod = isSupportedMethod;
            this.exceptionBaseType = compilation.GetTypeByMetadataName("System.Exception");
            this.nullableGuards = new NullableGuardDetector(compilation);
            this.operations = new MemberOperationEnumerator(compilation);
            this.recordedThrows = new RecordedThrowsReader(compilation, exceptionTypes);
        }

        public ImmutableArray<INamedTypeSymbol> GetImplicitDeclaredTypes(IMethodSymbol method)
        {
            if (method.DeclaringSyntaxReferences.Length == 0)
            {
                return this.recordedThrows.Read(method);
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
            if (!this.isSupportedMethod(method) || MethodAnalysisPolicy.IsGenerated(method))
            {
                return [];
            }

            var types = new List<INamedTypeSymbol>();
            foreach (IThrowOperation throwOperation in this.operations.EnumerateThrows(method))
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

        /// <summary>
        /// Resolves the thrown or rethrown type when it derives from <see cref="Exception"/> and
        /// no enclosing catch clause definitely handles it; otherwise <see langword="null"/>.
        /// </summary>
        internal ITypeSymbol? GetEscapingExceptionType(IThrowOperation throwOperation)
        {
            if (this.exceptionBaseType is null)
            {
                return null;
            }

            ITypeSymbol? exceptionType = throwOperation.Exception is null
                ? null
                : ExceptionFlowAnalysis.GetThrownExceptionType(throwOperation.Exception, this.compilation);
            exceptionType ??= ExceptionFlowAnalysis.GetRethrownExceptionType(throwOperation, this.compilation);
            return exceptionType is null ||
                !exceptionType.InheritsOrEquals(this.exceptionBaseType) ||
                ExceptionFlowAnalysis.IsCaught(throwOperation, exceptionType)
                ? null
                : exceptionType;
        }

        private ClassifiedThrow classify(IThrowOperation throwOperation, IMethodSymbol method)
        {
            if (this.exceptionBaseType is null ||
                OperationScope.IsHiddenFrom(throwOperation, method) ||
                !this.reachability.IsReachable(throwOperation))
            {
                return ClassifiedThrow.None;
            }

            if (this.GetEscapingExceptionType(throwOperation) is not INamedTypeSymbol named ||
                named.TypeKind == TypeKind.Error)
            {
                return ClassifiedThrow.None;
            }

            // Nullable annotations are not a runtime check. EXC007 still treats a
            // non-nullable reference parameter as a promise that the caller will not pass null.
            return this.nullableGuards.IsRedundantGuard(throwOperation, method, named)
                ? new ClassifiedThrow(GuardKind.RedundantNullable, named)
                : new ClassifiedThrow(GuardKind.ImplicitContract, named);
        }

        internal ImmutableArray<INamedTypeSymbol> GetObservedEscapingTypes(
            IMethodSymbol method,
            ContractResolver contracts,
            CancellationToken cancellationToken = default)
        {
            if (!this.isSupportedMethod(method) || MethodAnalysisPolicy.IsGenerated(method))
            {
                return [];
            }

            var types = new List<INamedTypeSymbol>();
            foreach (IThrowOperation throwOperation in this.operations.EnumerateThrows(method, cancellationToken))
            {
                ClassifiedThrow classified = this.classify(throwOperation, method);
                if (classified.ExceptionType is null ||
                    classified.Kind is not (GuardKind.ImplicitContract or GuardKind.RedundantNullable))
                {
                    continue;
                }

                addObserved(types, classified.ExceptionType);
            }

            foreach (IOperation root in this.operations.EnumerateRoots(method))
            {
                this.collectCallExceptions(root, method, contracts, types, cancellationToken);
            }

            return types.ToImmutableArray();
        }

        private void collectCallExceptions(
            IOperation operation,
            IMethodSymbol caller,
            ContractResolver contracts,
            List<INamedTypeSymbol> types,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (operation is INameOfOperation ||
                operation is IAnonymousFunctionOperation ||
                (operation is ILocalFunctionOperation localFunction && !localFunction.Symbol.IsSameMethod(caller)))
            {
                return;
            }

            if (!OperationScope.IsHiddenFrom(operation, caller) && this.reachability.IsReachable(operation))
            {
                this.collectTargets(operation, caller, contracts, types, OperationTargetResolver.GetTargetMethods(operation), false);
                this.collectTargets(operation, caller, contracts, types, OperationTargetResolver.GetPatternMethods(operation), true);
            }

            foreach (IOperation child in operation.ChildOperations)
            {
                this.collectCallExceptions(child, caller, contracts, types, cancellationToken);
            }
        }

        private void collectTargets(
            IOperation operation,
            IMethodSymbol caller,
            ContractResolver contracts,
            List<INamedTypeSymbol> types,
            IEnumerable<IMethodSymbol> targets,
            bool allowDeferredReturn)
        {
            foreach (IMethodSymbol target in targets)
            {
                if (target.IsSameMethod(caller) || !this.isAnalyzableTarget(target, operation, contracts, allowDeferredReturn))
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

        private bool isAnalyzableTarget(
            IMethodSymbol target,
            IOperation operation,
            ContractResolver contracts,
            bool allowDeferredReturn)
        {
            return CallCheck.IsChecked(target, operation, this.isSupportedMethod, contracts, allowDeferredReturn);
        }

        private static void addObserved(List<INamedTypeSymbol> types, INamedTypeSymbol exceptionType)
        {
            if (!types.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, exceptionType)))
            {
                types.Add(exceptionType);
            }
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