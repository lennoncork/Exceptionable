using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class OperationTargetResolver
    {
        internal static IEnumerable<IMethodSymbol> GetTargetMethods(IOperation operation)
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
                    foreach (IMethodSymbol accessor in getPropertyAccessors(propertyReference))
                    {
                        yield return accessor;
                    }

                    break;
                case IEventAssignmentOperation eventAssignment
                    when eventAssignment.EventReference is IEventReferenceOperation eventReference:
                    IMethodSymbol? eventAccessor = eventAssignment.Adds
                        ? eventReference.Event.AddMethod
                        : eventReference.Event.RemoveMethod;
                    if (eventAccessor is not null)
                    {
                        yield return eventAccessor;
                    }

                    break;
                case IBinaryOperation binary when binary.OperatorMethod is not null:
                    yield return binary.OperatorMethod;
                    break;
                case IUnaryOperation unary when unary.OperatorMethod is not null:
                    yield return unary.OperatorMethod;
                    break;
                case IIncrementOrDecrementOperation increment when increment.OperatorMethod is not null:
                    yield return increment.OperatorMethod;
                    break;
                case IConversionOperation conversion when conversion.OperatorMethod is not null:
                    yield return conversion.OperatorMethod;
                    break;
            }
        }

        internal static IEnumerable<IMethodSymbol> GetPatternMethods(IOperation operation)
        {
            if (operation.SemanticModel is not SemanticModel semanticModel)
            {
                yield break;
            }

            switch (operation)
            {
                case IForEachLoopOperation { IsAsynchronous: false }
                    when operation.Syntax is CommonForEachStatementSyntax forEachSyntax:
                    ForEachStatementInfo info = semanticModel.GetForEachStatementInfo(forEachSyntax);
                    if (info.GetEnumeratorMethod is not null)
                    {
                        yield return info.GetEnumeratorMethod;
                    }

                    if (info.MoveNextMethod is not null)
                    {
                        yield return info.MoveNextMethod;
                    }

                    if (info.CurrentProperty?.GetMethod is not null)
                    {
                        yield return info.CurrentProperty.GetMethod;
                    }

                    if (info.DisposeMethod is not null)
                    {
                        yield return info.DisposeMethod;
                    }

                    if (forEachSyntax is ForEachVariableStatementSyntax variableSyntax)
                    {
                        foreach (IMethodSymbol method in enumerateDeconstruction(semanticModel.GetDeconstructionInfo(variableSyntax)))
                        {
                            yield return method;
                        }
                    }

                    break;
                case IDeconstructionAssignmentOperation
                    when operation.Syntax is AssignmentExpressionSyntax assignment:
                    foreach (IMethodSymbol method in enumerateDeconstruction(semanticModel.GetDeconstructionInfo(assignment)))
                    {
                        yield return method;
                    }

                    break;
                case IUsingOperation { IsAsynchronous: false }:
                case IUsingDeclarationOperation { IsAsynchronous: false }:
                    foreach (IMethodSymbol method in getDisposeMethods(operation, semanticModel.Compilation))
                    {
                        yield return method;
                    }

                    break;
            }
        }

        private static IEnumerable<IMethodSymbol> getPropertyAccessors(IPropertyReferenceOperation propertyReference)
        {
            if (isDeconstructionTarget(propertyReference))
            {
                if (propertyReference.Property.SetMethod is not null)
                {
                    yield return propertyReference.Property.SetMethod;
                }

                yield break;
            }

            if (propertyReference.Parent is ISimpleAssignmentOperation assignment &&
                ReferenceEquals(assignment.Target, propertyReference))
            {
                if (propertyReference.Property.SetMethod is not null)
                {
                    yield return propertyReference.Property.SetMethod;
                }
            }
            else if ((propertyReference.Parent is ICompoundAssignmentOperation compoundAssignment &&
                        ReferenceEquals(compoundAssignment.Target, propertyReference)) ||
                (propertyReference.Parent is IIncrementOrDecrementOperation increment &&
                        ReferenceEquals(increment.Target, propertyReference)) ||
                (propertyReference.Parent is ICoalesceAssignmentOperation coalesceAssignment &&
                        ReferenceEquals(coalesceAssignment.Target, propertyReference)))
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
        }

        private static bool isDeconstructionTarget(IPropertyReferenceOperation propertyReference)
        {
            for (IOperation? current = propertyReference.Parent; current is not null; current = current.Parent)
            {
                if (current is IDeconstructionAssignmentOperation deconstruction)
                {
                    return isDescendant(propertyReference, deconstruction.Target);
                }
            }

            return false;
        }

        private static bool isDescendant(IOperation operation, IOperation ancestor)
        {
            for (IOperation? current = operation; current is not null; current = current.Parent)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<IMethodSymbol> enumerateDeconstruction(DeconstructionInfo info)
        {
            if (info.Method is not null)
            {
                yield return info.Method;
            }

            foreach (DeconstructionInfo nested in info.Nested)
            {
                foreach (IMethodSymbol method in enumerateDeconstruction(nested))
                {
                    yield return method;
                }
            }
        }

        private static IEnumerable<IMethodSymbol> getDisposeMethods(IOperation operation, Compilation compilation)
        {
            foreach (ITypeSymbol type in getResourceTypes(operation))
            {
                IMethodSymbol? method = findDisposeMethod(type, compilation);
                if (method is not null)
                {
                    yield return method;
                }
            }
        }

        private static IEnumerable<ITypeSymbol> getResourceTypes(IOperation operation)
        {
            switch (operation)
            {
                case IUsingOperation usingOperation:
                    foreach (ITypeSymbol type in getResourceTypes(usingOperation.Resources))
                    {
                        yield return type;
                    }

                    break;
                case IUsingDeclarationOperation usingDeclaration:
                    foreach (ITypeSymbol type in getResourceTypes(usingDeclaration.DeclarationGroup))
                    {
                        yield return type;
                    }

                    break;
                case IVariableDeclarationGroupOperation group:
                    foreach (IVariableDeclarationOperation declaration in group.Declarations)
                    {
                        foreach (ITypeSymbol type in getResourceTypes(declaration))
                        {
                            yield return type;
                        }
                    }

                    break;
                case IVariableDeclarationOperation declaration:
                    foreach (IVariableDeclaratorOperation declarator in declaration.Declarators)
                    {
                        if (declarator.Symbol?.Type is ITypeSymbol type)
                        {
                            yield return type;
                        }
                    }

                    break;
                default:
                    if (operation.Type is not null)
                    {
                        yield return operation.Type;
                    }

                    break;
            }
        }

        private static IMethodSymbol? findDisposeMethod(ITypeSymbol type, Compilation compilation)
        {
            type = unwrapNullable(type);
            for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
            {
                IMethodSymbol? method = current.GetMembers("Dispose").OfType<IMethodSymbol>()
                    .FirstOrDefault(isPublicParameterlessInstance);
                if (method is not null)
                {
                    return method;
                }
            }

            INamedTypeSymbol disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            if (disposable.TypeKind == TypeKind.Error)
            {
                return null;
            }

            IMethodSymbol? interfaceMethod = disposable.GetMembers("Dispose").OfType<IMethodSymbol>()
                .FirstOrDefault(candidate => candidate.Parameters.Length == 0);
            if (interfaceMethod is null ||
                !type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, disposable)))
            {
                return null;
            }

            if (type is INamedTypeSymbol named)
            {
                if (named.FindImplementationForInterfaceMember(interfaceMethod) is IMethodSymbol implementation)
                {
                    return implementation;
                }
            }

            return interfaceMethod;
        }

        private static ITypeSymbol unwrapNullable(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named &&
                named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
                named.TypeArguments.Length == 1)
            {
                return named.TypeArguments[0];
            }

            return type;
        }

        private static bool isPublicParameterlessInstance(IMethodSymbol method)
        {
            return method.Parameters.Length == 0 &&
                !method.IsStatic &&
                method.DeclaredAccessibility == Accessibility.Public;
        }
    }
}