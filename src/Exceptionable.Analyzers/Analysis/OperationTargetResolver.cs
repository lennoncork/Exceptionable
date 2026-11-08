using System.Collections.Generic;
using Microsoft.CodeAnalysis;
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
    }
}