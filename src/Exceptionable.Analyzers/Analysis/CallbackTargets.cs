using System.Collections.Generic;
using System.Collections.Immutable;
using Exceptionable.Analyzers.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    /// <summary>
    /// Resolves a delegate argument or a delegate invocation to the lambda, local function,
    /// method group, or parameter contract it names, when that target is unique.
    /// </summary>
    internal static class CallbackTargets
    {
        internal static bool TryGetEscapingExceptions(
            IOperation? value,
            IMethodSymbol scope,
            ExceptionableCompilationContext context,
            out ImmutableArray<INamedTypeSymbol> exceptions)
        {
            if (!TryResolve(value, scope, context, out CallbackBinding binding))
            {
                exceptions = [];
                return false;
            }

            exceptions = binding.GetEscapingExceptions(scope, context);
            return true;
        }

        internal static bool TryResolve(
            IOperation? value,
            IMethodSymbol scope,
            ExceptionableCompilationContext context,
            out CallbackBinding binding)
        {
            binding = CallbackBinding.Unknown;
            if (value is null)
            {
                return false;
            }

            value = unwrap(value);
            switch (value)
            {
                case IDelegateCreationOperation creation:
                    return TryResolve(creation.Target, scope, context, out binding);
                case IAnonymousFunctionOperation anonymous:
                    binding = CallbackBinding.ForAnonymous(anonymous);
                    return true;
                case IMethodReferenceOperation methodReference:
                    binding = CallbackBinding.ForMethod(methodReference.Method);
                    return true;
                case ILocalReferenceOperation local:
                    return tryGetLocalBinding(local.Local, scope, context, out binding);
                case IParameterReferenceOperation parameter
                    when isDelegate(parameter.Parameter.Type) &&
                        context.ContractResolver.GetDirectContract(parameter.Parameter).HasContract:
                    binding = CallbackBinding.ForParameter(context.ContractResolver.GetDirectContract(parameter.Parameter));
                    return true;
                default:
                    return false;
            }
        }

        internal static ImmutableArray<INamedTypeSymbol> GetEscapingExceptions(
            IAnonymousFunctionOperation anonymous,
            IMethodSymbol scope,
            ExceptionableCompilationContext context)
        {
            var types = new List<INamedTypeSymbol>();
            var stack = new List<IAnonymousFunctionOperation>();
            CollectAnonymous(anonymous, scope, context, types, stack);
            return types.ToImmutableArray();
        }

        internal static void CollectAnonymous(
            IAnonymousFunctionOperation anonymous,
            IMethodSymbol scope,
            ExceptionableCompilationContext context,
            List<INamedTypeSymbol> types,
            List<IAnonymousFunctionOperation> stack)
        {
            if (anonymous.Body is null || containsReference(stack, anonymous))
            {
                return;
            }

            stack.Add(anonymous);
            collect(anonymous.Body, scope, context, types, stack);
            stack.RemoveAt(stack.Count - 1);
        }

        private static bool tryGetLocalBinding(
            ILocalSymbol local,
            IMethodSymbol scope,
            ExceptionableCompilationContext context,
            out CallbackBinding binding)
        {
            for (IMethodSymbol? method = scope; method is not null; method = method.ContainingSymbol as IMethodSymbol)
            {
                if (context.GetDelegateBindings(method).TryGetValue(local, out binding))
                {
                    return binding.IsKnown;
                }
            }

            binding = CallbackBinding.Unknown;
            return false;
        }

        private static bool containsReference(
            List<IAnonymousFunctionOperation> stack,
            IAnonymousFunctionOperation anonymous)
        {
            foreach (IAnonymousFunctionOperation item in stack)
            {
                if (ReferenceEquals(item, anonymous))
                {
                    return true;
                }
            }

            return false;
        }

        private static void collect(
            IOperation operation,
            IMethodSymbol scope,
            ExceptionableCompilationContext context,
            List<INamedTypeSymbol> types,
            List<IAnonymousFunctionOperation> stack)
        {
            if (operation is INameOfOperation)
            {
                return;
            }

            if (operation is ILocalFunctionOperation or IAnonymousFunctionOperation)
            {
                return;
            }

            if (!context.Reachability.IsReachable(operation))
            {
                return;
            }

            if (operation is IThrowOperation throwOperation &&
                context.InferredThrows.GetEscapingExceptionType(throwOperation) is INamedTypeSymbol thrown)
            {
                add(types, thrown);
            }
            else if (operation is IInvocationOperation invocation &&
                invocation.TargetMethod.MethodKind == MethodKind.DelegateInvoke &&
                TryResolve(invocation.Instance, scope, context, out CallbackBinding nested) &&
                nested.IsKnown)
            {
                foreach (INamedTypeSymbol exceptionType in nested.GetEscapingExceptions(scope, context, stack))
                {
                    if (!ExceptionFlowAnalysis.IsCaught(invocation, exceptionType))
                    {
                        add(types, exceptionType);
                    }
                }
            }
            else
            {
                collectCalls(operation, scope, context, types, OperationTargetResolver.GetTargetMethods(operation), false);
                collectCalls(operation, scope, context, types, OperationTargetResolver.GetPatternMethods(operation), true);
            }

            foreach (IOperation child in operation.ChildOperations)
            {
                collect(child, scope, context, types, stack);
            }
        }

        private static void collectCalls(
            IOperation operation,
            IMethodSymbol scope,
            ExceptionableCompilationContext context,
            List<INamedTypeSymbol> types,
            IEnumerable<IMethodSymbol> targets,
            bool allowDeferredReturn)
        {
            foreach (IMethodSymbol target in targets)
            {
                if (target.IsSameMethod(scope) ||
                    !CallCheck.IsChecked(
                        target,
                        operation,
                        context.IsSupportedMethod,
                        context.ContractResolver,
                        allowDeferredReturn))
                {
                    continue;
                }

                ExceptionContract contract = context.ContractResolver.GetEffectiveContract(target);
                if (!contract.HasContract || contract.IsDoesNotThrow)
                {
                    continue;
                }

                foreach (INamedTypeSymbol exceptionType in contract.DeclaredTypes)
                {
                    if (!ExceptionFlowAnalysis.IsCaught(operation, exceptionType))
                    {
                        add(types, exceptionType);
                    }
                }
            }
        }

        private static void add(List<INamedTypeSymbol> types, INamedTypeSymbol exceptionType)
        {
            if (!types.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, exceptionType)))
            {
                types.Add(exceptionType);
            }
        }

        private static IOperation unwrap(IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }

            return operation;
        }

        internal static bool IsDelegateType(ITypeSymbol type) =>
            type is INamedTypeSymbol { TypeKind: TypeKind.Delegate };

        private static bool isDelegate(ITypeSymbol type) => IsDelegateType(type);
    }

    internal readonly struct CallbackBinding
    {
        internal static CallbackBinding Unknown { get; } = default;

        private readonly CallbackKind kind;
        private readonly IMethodSymbol? method;
        private readonly IAnonymousFunctionOperation? anonymous;
        private readonly ExceptionContract parameterContract;

        private CallbackBinding(
            CallbackKind kind,
            IMethodSymbol? method,
            IAnonymousFunctionOperation? anonymous,
            ExceptionContract parameterContract)
        {
            this.kind = kind;
            this.method = method;
            this.anonymous = anonymous;
            this.parameterContract = parameterContract;
        }

        internal bool IsKnown => this.kind != CallbackKind.Unknown;

        internal static CallbackBinding ForMethod(IMethodSymbol method) =>
            new(CallbackKind.Method, method, null, ExceptionContract.Unspecified);

        internal static CallbackBinding ForAnonymous(IAnonymousFunctionOperation anonymous) =>
            new(CallbackKind.Anonymous, null, anonymous, ExceptionContract.Unspecified);

        internal static CallbackBinding ForParameter(ExceptionContract contract) =>
            new(CallbackKind.Parameter, null, null, contract);

        internal bool SameAs(CallbackBinding other)
        {
            if (this.kind != other.kind)
            {
                return false;
            }

            return this.kind switch
            {
                CallbackKind.Method => this.method!.IsSameMethod(other.method!),
                CallbackKind.Anonymous => ReferenceEquals(this.anonymous, other.anonymous),
                CallbackKind.Parameter => this.parameterContract.Matches(other.parameterContract),
                _ => true,
            };
        }

        internal ImmutableArray<INamedTypeSymbol> GetEscapingExceptions(
            IMethodSymbol scope,
            ExceptionableCompilationContext context) =>
            this.GetEscapingExceptions(scope, context, []);

        internal ImmutableArray<INamedTypeSymbol> GetEscapingExceptions(
            IMethodSymbol scope,
            ExceptionableCompilationContext context,
            List<IAnonymousFunctionOperation> stack)
        {
            switch (this.kind)
            {
                case CallbackKind.Parameter:
                    return this.parameterContract.IsDoesNotThrow
                        ? []
                        : this.parameterContract.DeclaredTypes;
                case CallbackKind.Anonymous:
                    var types = new List<INamedTypeSymbol>();
                    CallbackTargets.CollectAnonymous(this.anonymous!, scope, context, types, stack);
                    return types.ToImmutableArray();
                case CallbackKind.Method:
                    ExceptionContract contract = context.ContractResolver.GetEffectiveContract(this.method!);
                    if (!contract.HasContract || contract.IsDoesNotThrow ||
                        !MethodAnalysisPolicy.HasExecutableBody(this.method!) &&
                        MethodAnalysisPolicy.IsDeferredReturnType(this.method!.ReturnType))
                    {
                        return [];
                    }

                    if (MethodAnalysisPolicy.IsDispatchUncertain(this.method!) &&
                        (!context.ContractResolver.GetDeclaredContract(this.method!).HasContract ||
                            context.ContractResolver.GetDeclaredContract(this.method!).IsDoesNotThrow))
                    {
                        return [];
                    }

                    return contract.DeclaredTypes;
                default:
                    return [];
            }
        }

        private enum CallbackKind
        {
            Unknown,
            Method,
            Anonymous,
            Parameter,
        }
    }

    internal sealed class DelegateBindingMap
    {
        private readonly Dictionary<ILocalSymbol, CallbackBinding> bindings =
            new(SymbolEqualityComparer.Default);

        internal static DelegateBindingMap Build(Compilation compilation, IMethodSymbol method)
        {
            var map = new DelegateBindingMap();
            var enumerator = new MemberOperationEnumerator(compilation);
            foreach (IOperation root in enumerator.EnumerateRoots(method))
            {
                map.walk(root, method);
            }

            return map;
        }

        internal bool TryGetValue(ILocalSymbol local, out CallbackBinding binding) =>
            this.bindings.TryGetValue(local, out binding);

        private void walk(IOperation operation, IMethodSymbol owner)
        {
            if (operation is INameOfOperation)
            {
                return;
            }

            if (operation is ILocalFunctionOperation localFunction && !localFunction.Symbol.IsSameMethod(owner))
            {
                return;
            }

            if (operation is IAnonymousFunctionOperation)
            {
                return;
            }

            switch (operation)
            {
                case IVariableDeclaratorOperation declarator
                    when declarator.Symbol is ILocalSymbol local && declarator.Initializer?.Value is IOperation initializer:
                    this.note(local, initializer);
                    break;
                case ISimpleAssignmentOperation { Target: ILocalReferenceOperation target } assignment:
                    this.note(target.Local, assignment.Value);
                    break;
            }

            foreach (IOperation child in operation.ChildOperations)
            {
                this.walk(child, owner);
            }
        }

        private void note(ILocalSymbol local, IOperation value)
        {
            if (!tryResolveWithoutContext(value, out CallbackBinding binding) || !binding.IsKnown)
            {
                this.markUnknown(local);
                return;
            }

            if (this.bindings.TryGetValue(local, out CallbackBinding existing))
            {
                if (!existing.IsKnown || !existing.SameAs(binding))
                {
                    this.bindings[local] = CallbackBinding.Unknown;
                }

                return;
            }

            this.bindings[local] = binding;
        }

        private void markUnknown(ILocalSymbol local) => this.bindings[local] = CallbackBinding.Unknown;

        private static bool tryResolveWithoutContext(IOperation value, out CallbackBinding binding)
        {
            binding = CallbackBinding.Unknown;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }

            switch (value)
            {
                case IDelegateCreationOperation creation:
                    return tryResolveWithoutContext(creation.Target, out binding);
                case IAnonymousFunctionOperation anonymous:
                    binding = CallbackBinding.ForAnonymous(anonymous);
                    return true;
                case IMethodReferenceOperation methodReference:
                    binding = CallbackBinding.ForMethod(methodReference.Method);
                    return true;
                default:
                    return false;
            }
        }
    }
}
