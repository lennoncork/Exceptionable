using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers
{
    /// <summary>Analyzes explicitly declared exception contracts.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExceptionableAnalyzer : DiagnosticAnalyzer
    {
        private const string doesNotThrowAttributeName = "Exceptionable.DoesNotThrowAttribute";
        private const string throwsAttributeName = "Exceptionable.ThrowsAttribute";

        private static readonly DiagnosticDescriptor doesNotThrowEscapes = new(
            "EXC001",
            "Exception escapes a [DoesNotThrow] contract",
            "Exception type '{0}' escapes a method marked [DoesNotThrow]",
            "Exceptionable",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC001.md");

        private static readonly DiagnosticDescriptor throwsMismatch = new(
            "EXC002",
            "Exception escapes a declared contract",
            "Exception type '{0}' escapes a method but is not declared by its contract",
            "Exceptionable",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC002.md");

        private static readonly DiagnosticDescriptor callerViolation = new(
            "EXC003",
            "Call to annotated method may throw an unhandled exception",
            "Call to '{0}' may throw '{1}' which is neither caught nor permitted by the caller's contract",
            "Exceptionable",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC003.md");

        private static readonly DiagnosticDescriptor throwsUsage = new(
            "EXC004",
            "Invalid Throws attribute name",
            "The type name '{0}' in [Throws] is not a valid exception type in this compilation",
            "Exceptionable",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC004.md");

        private static readonly DiagnosticDescriptor conflictingContracts = new(
            "EXC005",
            "Conflicting exception attributes",
            "A member cannot be annotated with both [DoesNotThrow] and [Throws]",
            "Exceptionable",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC005.md");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(doesNotThrowEscapes, throwsMismatch, callerViolation, throwsUsage, conflictingContracts);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var compilation = startContext.Compilation;
                var optionsProvider = startContext.Options.AnalyzerConfigOptionsProvider;
                var methodReachability = new ConcurrentDictionary<IMethodSymbol, MethodReachability>(
                    SymbolEqualityComparer.Default);

                startContext.RegisterOperationAction(operationContext =>
                {
                    if (operationContext.Operation is not IThrowOperation throwOperation ||
                        !isEnabled(optionsProvider, throwOperation.Syntax.SyntaxTree) ||
                        operationContext.ContainingSymbol is not IMethodSymbol containingMethod ||
                        !isSupportedMethod(containingMethod) ||
                        !isReachable(throwOperation, containingMethod, methodReachability))
                    {
                        return;
                    }

                    var exceptionType = throwOperation.Exception is null
                        ? null
                        : getThrownExceptionType(throwOperation.Exception);
                    if (exceptionType is null)
                    {
                        exceptionType = getRethrownExceptionType(throwOperation, compilation);
                    }

                    var exceptionBaseType = compilation.GetTypeByMetadataName("System.Exception");
                    if (exceptionType is null || exceptionBaseType is null ||
                        !inheritsOrEquals(exceptionType, exceptionBaseType))
                    {
                        return;
                    }

                    var contract = getEffectiveContract(containingMethod, compilation);
                    var interfaceContracts = getImplementedInterfaceContracts(containingMethod, compilation);
                    if ((!contract.HasContract && interfaceContracts.Length == 0) ||
                        isCaught(throwOperation, exceptionType))
                    {
                        return;
                    }

                    if (contract.IsDoesNotThrow || interfaceContracts.Any(interfaceContract => interfaceContract.IsDoesNotThrow))
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(
                            doesNotThrowEscapes,
                            throwOperation.Syntax.GetLocation(),
                            exceptionType.Name));
                    }
                    else if ((contract.HasContract &&
                                !contract.DeclaredTypes.Any(declaredType => inheritsOrEquals(exceptionType, declaredType))) ||
                        interfaceContracts.Any(interfaceContract =>
                            !interfaceContract.IsDoesNotThrow &&
                            !interfaceContract.DeclaredTypes.Any(declaredType => inheritsOrEquals(exceptionType, declaredType))))
                    {
                        operationContext.ReportDiagnostic(Diagnostic.Create(
                            throwsMismatch,
                            throwOperation.Syntax.GetLocation(),
                            exceptionType.Name));
                    }
                }, OperationKind.Throw);

                startContext.RegisterOperationAction(operationContext =>
                {
                    if (!isEnabled(optionsProvider, operationContext.Operation.Syntax.SyntaxTree) ||
                        operationContext.ContainingSymbol is not IMethodSymbol callerMethod ||
                        !isSupportedMethod(callerMethod) ||
                        !isReachable(operationContext.Operation, callerMethod, methodReachability))
                    {
                        return;
                    }

                    var callerContract = getEffectiveContract(callerMethod, compilation);
                    foreach (var targetMethod in getTargetMethods(operationContext.Operation))
                    {
                        if ((isDispatchUncertain(targetMethod) &&
                                targetMethod.ContainingType.TypeKind != TypeKind.Interface) ||
                            !isSupportedMethod(targetMethod) ||
                            isDeferredReturnType(targetMethod.ReturnType))
                        {
                            continue;
                        }

                        var targetContract = getEffectiveContract(targetMethod, compilation);
                        foreach (var declaredType in targetContract.DeclaredTypes)
                        {
                            if (callerContract.DeclaredTypes.Any(type => inheritsOrEquals(declaredType, type)) ||
                                isCaught(operationContext.Operation, declaredType))
                            {
                                continue;
                            }

                            operationContext.ReportDiagnostic(Diagnostic.Create(
                                callerViolation,
                                operationContext.Operation.Syntax.GetLocation(),
                                targetMethod.Name,
                                declaredType.Name));
                        }
                    }
                }, OperationKind.Invocation, OperationKind.ObjectCreation, OperationKind.PropertyReference, OperationKind.EventAssignment);

                startContext.RegisterSymbolAction(symbolContext =>
                {
                    if (symbolContext.Symbol is IMethodSymbol method)
                    {
                        validateContract(symbolContext, method, compilation);
                    }
                    else if (symbolContext.Symbol is INamedTypeSymbol type)
                    {
                        validateContract(symbolContext, type, compilation);
                    }
                }, SymbolKind.Method, SymbolKind.NamedType);

                void validateContract(SymbolAnalysisContext symbolContext, ISymbol symbol, Compilation currentCompilation)
                {
                    var syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxReference is null)
                    {
                        return;
                    }

                    var attributes = symbol.GetAttributes();
                    var hasDoesNotThrow = attributes.Any(attribute => isAttribute(attribute, doesNotThrowAttributeName));
                    var throwsAttributes = attributes.Where(attribute => isAttribute(attribute, throwsAttributeName)).ToImmutableArray();

                    if (hasDoesNotThrow && throwsAttributes.Length > 0)
                    {
                        var attributeReference = throwsAttributes
                            .Select(attribute => attribute.ApplicationSyntaxReference)
                            .FirstOrDefault(reference => reference is not null &&
                                isEnabled(optionsProvider, reference.SyntaxTree));
                        if (attributeReference is not null)
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(
                                conflictingContracts,
                                attributeReference.GetSyntax().GetLocation()));
                        }
                    }

                    foreach (var attribute in throwsAttributes)
                    {
                        var attributeReference = attribute.ApplicationSyntaxReference;
                        if (attributeReference is null || !isEnabled(optionsProvider, attributeReference.SyntaxTree))
                        {
                            continue;
                        }

                        if (!tryGetThrowsName(attribute, out var throwsName) ||
                            resolveExceptionType(currentCompilation, throwsName) is null)
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(
                                throwsUsage,
                                attributeReference.GetSyntax().GetLocation(),
                                getThrowsName(attribute)));
                        }
                    }
                }
            });
        }

        private static bool isEnabled(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
        {
            var treeOptions = provider.GetOptions(tree);
            if (treeOptions.TryGetValue("exceptionable_analysis", out var treeValue))
            {
                return string.Equals(treeValue, "enable", StringComparison.OrdinalIgnoreCase);
            }

            return provider.GlobalOptions.TryGetValue("build_property.Exceptionable", out var globalValue) &&
                string.Equals(globalValue, "enable", StringComparison.OrdinalIgnoreCase);
        }

        private static bool isSupportedMethod(IMethodSymbol method)
        {
            if (method.IsAsync ||
                method.MethodKind == MethodKind.AnonymousFunction ||
                method.MethodKind == MethodKind.LocalFunction ||
                method.MethodKind == MethodKind.UserDefinedOperator ||
                method.MethodKind == MethodKind.Conversion)
            {
                return false;
            }

            foreach (var syntaxReference in method.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax().DescendantNodesAndSelf().OfType<YieldStatementSyntax>().Any())
                {
                    return false;
                }
            }

            return true;
        }

        private static bool isDeferredReturnType(ITypeSymbol returnType)
        {
            if (returnType is not INamedTypeSymbol namedType)
            {
                return false;
            }

            var namespaceName = namedType.ContainingNamespace.ToDisplayString();
            return (namespaceName == "System.Threading.Tasks" &&
                    (namedType.Name == "Task" || namedType.Name == "ValueTask")) ||
                (namespaceName == "System.Collections" &&
                    (namedType.Name == "IEnumerable" || namedType.Name == "IEnumerator")) ||
                (namespaceName == "System.Collections.Generic" &&
                    (namedType.Name == "IEnumerable" || namedType.Name == "IEnumerator" ||
                        namedType.Name == "IAsyncEnumerable" || namedType.Name == "IAsyncEnumerator"));
        }

        private static bool isDispatchUncertain(IMethodSymbol method) =>
            method.IsVirtual || method.IsAbstract || method.IsOverride;

        private static bool isReachable(
            IOperation operation,
            IMethodSymbol method,
            ConcurrentDictionary<IMethodSymbol, MethodReachability> methodReachability)
        {
            return methodReachability
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

        private static IEnumerable<IMethodSymbol> getTargetMethods(IOperation operation)
        {
            switch (operation)
            {
                case IInvocationOperation invocation:
                    yield return invocation.TargetMethod;
                    break;
                case IObjectCreationOperation objectCreation when objectCreation.Constructor is not null:
                    yield return objectCreation.Constructor;
                    break;
                case IPropertyReferenceOperation propertyReference:
                    if (propertyReference.Parent is ISimpleAssignmentOperation assignment &&
                        ReferenceEquals(assignment.Target, propertyReference))
                    {
                        if (propertyReference.Property.SetMethod is not null)
                        {
                            yield return propertyReference.Property.SetMethod;
                        }
                    }
                    else if (propertyReference.Parent is ICompoundAssignmentOperation compoundAssignment &&
                        ReferenceEquals(compoundAssignment.Target, propertyReference) ||
                        propertyReference.Parent is IIncrementOrDecrementOperation increment &&
                        ReferenceEquals(increment.Target, propertyReference) ||
                        propertyReference.Parent is ICoalesceAssignmentOperation coalesceAssignment &&
                        ReferenceEquals(coalesceAssignment.Target, propertyReference))
                    {
                        if (propertyReference.Property.GetMethod is not null)
                        {
                            yield return propertyReference.Property.GetMethod;
                        }

                        if (propertyReference.Property.SetMethod is not null)
                        {
                            yield return propertyReference.Property.SetMethod;
                        }
                    }
                    else if (propertyReference.Property.GetMethod is not null)
                    {
                        yield return propertyReference.Property.GetMethod;
                    }

                    break;
                case IEventAssignmentOperation eventAssignment
                    when eventAssignment.EventReference is IEventReferenceOperation eventReference:
                    var accessor = eventAssignment.Adds
                        ? eventReference.Event.AddMethod
                        : eventReference.Event.RemoveMethod;
                    if (accessor is not null)
                    {
                        yield return accessor;
                    }

                    break;
            }
        }

        private static ContractInfo getEffectiveContract(IMethodSymbol method, Compilation compilation)
        {
            for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
            {
                var methodContract = getDirectContract(current, compilation);
                if (methodContract.HasContract)
                {
                    return methodContract;
                }
            }

            for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.BaseType)
            {
                var typeContract = getDirectContract(type, compilation);
                if (typeContract.HasContract)
                {
                    return typeContract;
                }
            }

            return ContractInfo.Unspecified;
        }

        private static ImmutableArray<ContractInfo> getImplementedInterfaceContracts(
            IMethodSymbol method,
            Compilation compilation)
        {
            if (method.ContainingType.TypeKind == TypeKind.Interface)
            {
                return ImmutableArray<ContractInfo>.Empty;
            }

            var interfaceMembers = method.ExplicitInterfaceImplementations
                .Concat(method.ContainingType.AllInterfaces.SelectMany(type => type.GetMembers().OfType<IMethodSymbol>()));
            var seenMembers = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var contracts = ImmutableArray.CreateBuilder<ContractInfo>();

            foreach (var interfaceMember in interfaceMembers)
            {
                if (!seenMembers.Add(interfaceMember))
                {
                    continue;
                }

                var implementation = method.ContainingType.FindImplementationForInterfaceMember(interfaceMember) as IMethodSymbol;
                if (implementation is null || !sameMethod(implementation, method))
                {
                    continue;
                }

                var contract = getEffectiveContract(interfaceMember, compilation);
                if (contract.HasContract)
                {
                    contracts.Add(contract);
                }
            }

            return contracts.ToImmutable();
        }

        private static bool sameMethod(IMethodSymbol first, IMethodSymbol second) =>
            SymbolEqualityComparer.Default.Equals(first, second) ||
            SymbolEqualityComparer.Default.Equals(first.OriginalDefinition, second.OriginalDefinition);

        private static ContractInfo getDirectContract(ISymbol symbol, Compilation compilation)
        {
            var attributes = symbol.GetAttributes();
            var doesNotThrow = attributes.Any(attribute => isAttribute(attribute, doesNotThrowAttributeName));
            var declaredTypes = attributes
                .Where(attribute => isAttribute(attribute, throwsAttributeName))
                .Select(getThrowsName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => resolveExceptionType(compilation, name))
                .Where(type => type is not null)
                .Cast<INamedTypeSymbol>()
                .ToImmutableArray();

            return doesNotThrow
                ? new ContractInfo(true, ImmutableArray<INamedTypeSymbol>.Empty)
                : declaredTypes.Length > 0
                    ? new ContractInfo(false, declaredTypes)
                    : ContractInfo.Unspecified;
        }

        private static bool isAttribute(AttributeData attribute, string metadataName) =>
            attribute.AttributeClass?.ToDisplayString() == metadataName;

        private static string getThrowsName(AttributeData attribute) =>
            tryGetThrowsName(attribute, out var name) ? name : string.Empty;

        private static bool tryGetThrowsName(AttributeData attribute, out string name)
        {
            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string value &&
                !string.IsNullOrWhiteSpace(value))
            {
                name = value.Trim();
                return true;
            }

            name = string.Empty;
            return false;
        }

        private static bool isCaught(IOperation operation, ITypeSymbol exceptionType)
        {
            for (var current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current is not ITryOperation tryOperation ||
                    tryOperation.Catches.Any(catchClause => isDescendantOf(operation, catchClause.Handler)) ||
                    !isDescendantOf(operation, tryOperation.Body))
                {
                    continue;
                }

                foreach (var catchClause in tryOperation.Catches)
                {
                    if (!isFilterDefinitelyTrue(catchClause.Filter) ||
                        (catchClause.ExceptionType is not null &&
                            !inheritsOrEquals(exceptionType, catchClause.ExceptionType)))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool isDescendantOf(IOperation operation, IOperation ancestor)
        {
            for (var current = operation; current is not null; current = current.Parent)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool isFilterDefinitelyTrue(IOperation? filter)
        {
            if (filter is null)
            {
                return true;
            }

            var constantValue = filter.ConstantValue;
            return constantValue.HasValue && constantValue.Value is true;
        }

        private static ITypeSymbol? getRethrownExceptionType(IThrowOperation throwOperation, Compilation compilation)
        {
            for (var current = throwOperation.Parent; current is not null; current = current.Parent)
            {
                if (current is ICatchClauseOperation catchClause &&
                    isDescendantOf(throwOperation, catchClause.Handler))
                {
                    if (catchClause.Syntax is CatchClauseSyntax rethrowCatchSyntax &&
                        rethrowCatchSyntax.Declaration is null)
                    {
                        return compilation.GetTypeByMetadataName("System.Exception");
                    }

                    return catchClause.ExceptionType ?? compilation.GetTypeByMetadataName("System.Exception");
                }
            }

            var catchClauseSyntax = throwOperation.Syntax.AncestorsAndSelf()
                .OfType<CatchClauseSyntax>()
                .FirstOrDefault();
            if (catchClauseSyntax is null ||
                !catchClauseSyntax.Block.Span.Contains(throwOperation.Syntax.Span))
            {
                return null;
            }

            if (catchClauseSyntax.Declaration is null)
            {
                return compilation.GetTypeByMetadataName("System.Exception");
            }

            return null;
        }

        private static ITypeSymbol? getThrownExceptionType(IOperation exception)
        {
            while (exception is IConversionOperation conversion && conversion.IsImplicit)
            {
                exception = conversion.Operand;
            }

            return exception.Type;
        }

        private static bool inheritsOrEquals(ITypeSymbol actual, ITypeSymbol expected)
        {
            for (ITypeSymbol? current = actual; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, expected))
                {
                    return true;
                }
            }

            return false;
        }

        private static INamedTypeSymbol? resolveExceptionType(Compilation compilation, string value)
        {
            var trimmed = value.Trim();
            var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
            if (string.Equals(trimmed, "Exception", StringComparison.Ordinal))
            {
                return exceptionType;
            }

            INamedTypeSymbol? resolved;
            if (trimmed.IndexOf('.') >= 0)
            {
                resolved = compilation.GetTypeByMetadataName(trimmed);
            }
            else
            {
                var matches = findExceptionTypes(compilation.GlobalNamespace, trimmed)
                    .Take(2)
                    .ToArray();
                resolved = matches.Length == 1 ? matches[0] : null;
            }

            return resolved is not null && exceptionType is not null && inheritsOrEquals(resolved, exceptionType)
                ? resolved
                : null;
        }

        private static IEnumerable<INamedTypeSymbol> findExceptionTypes(INamespaceSymbol namespaceSymbol, string name)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                foreach (var match in findExceptionTypes(type, name))
                {
                    yield return match;
                }
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var match in findExceptionTypes(nestedNamespace, name))
                {
                    yield return match;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> findExceptionTypes(INamedTypeSymbol type, string name)
        {
            if (string.Equals(type.Name, name, StringComparison.Ordinal))
            {
                yield return type;
            }

            foreach (var nestedType in type.GetTypeMembers())
            {
                foreach (var match in findExceptionTypes(nestedType, name))
                {
                    yield return match;
                }
            }
        }

        private readonly struct ContractInfo
        {
            public static ContractInfo Unspecified { get; } =
                new(false, ImmutableArray<INamedTypeSymbol>.Empty);

            public ContractInfo(bool isDoesNotThrow, ImmutableArray<INamedTypeSymbol> declaredTypes)
            {
                this.IsDoesNotThrow = isDoesNotThrow;
                this.DeclaredTypes = declaredTypes;
            }

            public bool IsDoesNotThrow { get; }

            public ImmutableArray<INamedTypeSymbol> DeclaredTypes { get; }

            public bool HasContract => this.IsDoesNotThrow || this.DeclaredTypes.Length > 0;
        }

        private sealed class MethodReachability
        {
            public static MethodReachability Unavailable { get; } = new(
                new Dictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool>(),
                isAvailable: false);

            private readonly IReadOnlyDictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool> operationReachability;
            private readonly bool isAvailable;

            public MethodReachability(
                IReadOnlyDictionary<(SyntaxTree Tree, Microsoft.CodeAnalysis.Text.TextSpan Span), bool> operationReachability,
                bool isAvailable = true)
            {
                this.operationReachability = operationReachability;
                this.isAvailable = isAvailable;
            }

            public bool IsReachable(IOperation operation)
            {
                var key = (operation.Syntax.SyntaxTree, operation.Syntax.Span);
                if (!this.isAvailable)
                {
                    return true;
                }

                if (this.operationReachability.TryGetValue(key, out var isReachable))
                {
                    return isReachable;
                }

                return true;
            }
        }
    }
}