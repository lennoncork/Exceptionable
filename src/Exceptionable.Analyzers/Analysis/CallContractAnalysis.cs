using System.Collections.Generic;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class CallContractAnalysis
    {
        internal static void Analyze(
            OperationAnalysisContext operationContext,
            ExceptionableCompilationContext compilationContext)
        {
            IOperation operation = operationContext.Operation;
            if (!compilationContext.Options.IsEnabled(operation.Syntax.SyntaxTree) ||
                OperationScope.IsIgnored(operation))
            {
                return;
            }

            var targets = new List<(IMethodSymbol Method, bool AllowDeferredReturn)>();
            foreach (IMethodSymbol target in OperationTargetResolver.GetTargetMethods(operation))
            {
                targets.Add((target, false));
            }

            foreach (IMethodSymbol target in OperationTargetResolver.GetPatternMethods(operation))
            {
                targets.Add((target, true));
            }

            if (targets.Count == 0)
            {
                return;
            }

            IMethodSymbol[] hosts = [.. ConstructorHost.GetMethods(operationContext.ContainingSymbol)
                .Where(compilationContext.IsSupportedMethod)
                .Where(host => compilationContext.Reachability.IsReachable(operation))];
            if (hosts.Length == 0)
            {
                return;
            }

            ContractResolver contractResolver = compilationContext.ContractResolver;
            var reported = new HashSet<string>();
            foreach ((IMethodSymbol? targetMethod, bool allowDeferredReturn) in targets)
            {
                if (!isAnalyzableTarget(targetMethod, operation, compilationContext, allowDeferredReturn))
                {
                    continue;
                }

                ExceptionContract targetContract = contractResolver.GetEffectiveContract(targetMethod);
                foreach (INamedTypeSymbol declaredType in targetContract.DeclaredTypes)
                {
                    if (hosts.All(host =>
                            contractResolver.IsExceptionPermitted(host, declaredType) ||
                            ExceptionFlowAnalysis.IsCaught(operation, declaredType)))
                    {
                        continue;
                    }

                    string key = targetMethod.GetContractDisplayName() + ":" + exceptionIdentity(declaredType);
                    if (!reported.Add(key))
                    {
                        continue;
                    }

                    operationContext.ReportDiagnostic(Diagnostic.Create(
                        ExceptionableDiagnostics.CallerViolation,
                        getReportLocation(operation),
                        targetMethod.GetContractDisplayName(),
                        declaredType.GetExceptionDisplayName()));
                }
            }
        }

        private static string exceptionIdentity(ITypeSymbol type) =>
            type.GetExceptionDisplayName() + ":" + type.ContainingAssembly?.Identity.Name;

        private static Location getReportLocation(IOperation operation)
        {
            return operation.Syntax switch
            {
                CommonForEachStatementSyntax forEach => forEach.ForEachKeyword.GetLocation(),
                UsingStatementSyntax usingStatement => usingStatement.UsingKeyword.GetLocation(),
                LocalDeclarationStatementSyntax local when local.UsingKeyword != default =>
                    local.UsingKeyword.GetLocation(),
                _ => operation.Syntax.GetLocation(),
            };
        }

        private static bool isAnalyzableTarget(
            IMethodSymbol target,
            IOperation operation,
            ExceptionableCompilationContext compilationContext,
            bool allowDeferredReturn)
        {
            if (!MethodAnalysisPolicy.HasCertainDispatch(target, operation) ||
                !compilationContext.IsSupportedMethod(target))
            {
                return false;
            }

            return allowDeferredReturn || !MethodAnalysisPolicy.IsDeferredReturnType(target.ReturnType);
        }
    }
}