using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class ReachabilityAnalysis
    {
        private readonly ConcurrentDictionary<(SyntaxTree Tree, TextSpan Span), MethodReachability> graphs = new();

        internal bool IsReachable(IOperation operation)
        {
            if (!tryGetControlFlowRoot(operation, out IOperation? root))
            {
                return true;
            }

            (SyntaxTree SyntaxTree, TextSpan Span) key = (root.Syntax.SyntaxTree, root.Syntax.Span);
            return this.graphs
                .GetOrAdd(key, _ => createMethodReachability(root))
                .IsReachable(operation);
        }

        private static bool tryGetControlFlowRoot(IOperation operation, out IOperation root)
        {
            for (IOperation? current = operation; current is not null; current = current.Parent)
            {
                if (current is IMethodBodyOperation or IConstructorBodyOperation ||
                    (current is IBlockOperation block && block.Parent is null))
                {
                    root = current;
                    return true;
                }
            }

            root = operation;
            return false;
        }

        private static MethodReachability createMethodReachability(IOperation root)
        {
            ControlFlowGraph? controlFlowGraph = root switch
            {
                IMethodBodyOperation methodBody => ControlFlowGraph.Create(methodBody),
                IConstructorBodyOperation constructorBody => ControlFlowGraph.Create(constructorBody),
                IBlockOperation blockBody when blockBody.Parent is null => ControlFlowGraph.Create(blockBody),
                _ => null,
            };

            if (controlFlowGraph is not null)
            {

                var reachability = new Dictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool>();
                foreach (BasicBlock block in controlFlowGraph.Blocks)
                {
                    foreach (IOperation blockOperation in block.Operations)
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
            for (SyntaxNode? syntax = operation.Syntax; syntax is not null; syntax = syntax.Parent)
            {
                (SyntaxTree SyntaxTree, TextSpan Span) key = (syntax.SyntaxTree, syntax.Span);
                reachability[key] = reachability.TryGetValue(key, out bool existing)
                    ? existing || isBlockReachable
                    : isBlockReachable;
            }

            foreach (IOperation child in operation.ChildOperations)
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
                (SyntaxTree SyntaxTree, TextSpan Span) key = (operation.Syntax.SyntaxTree, operation.Syntax.Span);
                if (!this.isAvailable)
                {
                    return true;
                }

                return !this.operationReachability.TryGetValue(key, out bool isReachable) || isReachable;
            }
        }
    }
}