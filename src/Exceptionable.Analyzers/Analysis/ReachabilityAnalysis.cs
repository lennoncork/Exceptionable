using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class ReachabilityAnalysis
    {
        private readonly ConcurrentDictionary<IMethodSymbol, MethodReachability> methodReachability =
            new(SymbolEqualityComparer.Default);

        internal bool IsReachable(IOperation operation, IMethodSymbol method)
        {
            return this.methodReachability
                .GetOrAdd(method, _ => createMethodReachability(operation))
                .IsReachable(operation);
        }

        private static MethodReachability createMethodReachability(IOperation operation)
        {
            for (var current = operation; current is not null; current = current.Parent)
            {
                var controlFlowGraph = current switch
                {
                    IMethodBodyOperation methodBody => ControlFlowGraph.Create(methodBody),
                    IConstructorBodyOperation constructorBody => ControlFlowGraph.Create(constructorBody),
                    IBlockOperation blockBody when blockBody.Parent is null => ControlFlowGraph.Create(blockBody),
                    _ => null
                };

                if (controlFlowGraph is null)
                {
                    continue;
                }

                var reachability = new Dictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool>();
                foreach (var block in controlFlowGraph.Blocks)
                {
                    foreach (var blockOperation in block.Operations)
                    {
                        addReachability(blockOperation, block.IsReachable, reachability);
                    }

                    if (block.BranchValue is not null)
                    {
                        addReachability(block.BranchValue, block.IsReachable, reachability);
                    }
                }

                return new MethodReachability(reachability);
            }

            return MethodReachability.Unavailable;
        }

        private static void addReachability(
            IOperation operation,
            bool isBlockReachable,
            IDictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool> reachability)
        {
            for (var syntax = operation.Syntax; syntax is not null; syntax = syntax.Parent)
            {
                var key = (syntax.SyntaxTree, syntax.Span);
                reachability[key] = reachability.TryGetValue(key, out var existing)
                    ? existing || isBlockReachable
                    : isBlockReachable;
            }

            foreach (var child in operation.ChildOperations)
            {
                addReachability(child, isBlockReachable, reachability);
            }
        }

        private sealed class MethodReachability
        {
            internal static MethodReachability Unavailable { get; } = new(
                new Dictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool>(),
                isAvailable: false);

            private readonly IReadOnlyDictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool>
                operationReachability;
            private readonly bool isAvailable;

            internal MethodReachability(
                IReadOnlyDictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool>
                    operationReachability,
                bool isAvailable = true)
            {
                this.operationReachability = operationReachability;
                this.isAvailable = isAvailable;
            }

            internal bool IsReachable(IOperation operation)
            {
                var key = (operation.Syntax.SyntaxTree, operation.Syntax.Span);
                if (!this.isAvailable)
                {
                    return true;
                }

                return this.operationReachability.TryGetValue(key, out var isReachable)
                    ? isReachable
                    : true;
            }
        }
    }
}