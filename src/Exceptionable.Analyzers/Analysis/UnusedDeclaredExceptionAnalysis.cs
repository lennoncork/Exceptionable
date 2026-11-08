using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class UnusedDeclaredExceptionAnalysis
    {
        internal static void Analyze(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext)
        {
            switch (symbolContext.Symbol)
            {
                case IMethodSymbol method:
                    analyzeMethod(symbolContext, compilationContext, method);
                    break;
                case IPropertySymbol property:
                    analyzeAccessors(
                        symbolContext,
                        compilationContext,
                        property,
                        property.GetMethod,
                        property.SetMethod);
                    break;
                case IEventSymbol eventSymbol:
                    analyzeAccessors(
                        symbolContext,
                        compilationContext,
                        eventSymbol,
                        eventSymbol.AddMethod,
                        eventSymbol.RemoveMethod);
                    break;
                case INamedTypeSymbol type:
                    analyzeType(symbolContext, compilationContext, type);
                    break;
            }
        }

        private static void analyzeMethod(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext,
            IMethodSymbol method)
        {
            if (method.AssociatedSymbol is not null &&
                !compilationContext.ContractResolver.GetDirectContract(method).HasContract)
            {
                return;
            }

            reportUncovered(symbolContext, compilationContext, method, [method]);
        }

        private static void analyzeAccessors(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext,
            ISymbol owner,
            params IMethodSymbol?[] accessors)
        {
            var governed = new List<IMethodSymbol>();
            foreach (IMethodSymbol? accessor in accessors)
            {
                if (accessor is null || compilationContext.ContractResolver.GetDirectContract(accessor).HasContract)
                {
                    continue;
                }

                governed.Add(accessor);
            }

            reportUncovered(symbolContext, compilationContext, owner, governed);
        }

        private static void analyzeType(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext,
            INamedTypeSymbol type)
        {
            var governed = new List<IMethodSymbol>();
            foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
            {
                if (usesOwnOrAssociatedContract(compilationContext, method))
                {
                    continue;
                }

                governed.Add(method);
            }

            reportUncovered(symbolContext, compilationContext, type, governed);
        }

        private static bool usesOwnOrAssociatedContract(
            ExceptionableCompilationContext compilationContext,
            IMethodSymbol method)
        {
            ContractResolver contracts = compilationContext.ContractResolver;
            if (contracts.GetDirectContract(method).HasContract)
            {
                return true;
            }

            return method.AssociatedSymbol is not null &&
                contracts.GetDirectContract(method.AssociatedSymbol).HasContract;
        }

        private static void reportUncovered(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext,
            ISymbol owner,
            IReadOnlyList<IMethodSymbol> methods)
        {
            ExceptionContract contract = compilationContext.ContractResolver.GetDirectContract(owner);
            if (contract.IsDoesNotThrow || contract.DeclaredTypes.IsEmpty)
            {
                return;
            }

            var observed = new List<INamedTypeSymbol>();
            foreach (IMethodSymbol method in methods)
            {
                foreach (INamedTypeSymbol exceptionType in compilationContext.InferredThrows.GetObservedEscapingTypes(
                    method,
                    compilationContext.ContractResolver))
                {
                    if (!observed.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, exceptionType)))
                    {
                        observed.Add(exceptionType);
                    }
                }
            }

            if (observed.Count == 0)
            {
                return;
            }

            foreach (AttributeData attribute in owner.GetAttributes().Where(ThrowsAttributeReader.IsThrows))
            {
                if (!tryGetDeclaredException(attribute, compilationContext, out INamedTypeSymbol? declared) ||
                    declared is null ||
                    observed.Exists(escaping => escaping.InheritsOrEquals(declared)))
                {
                    continue;
                }

                SyntaxReference? reference = attribute.ApplicationSyntaxReference;
                if (reference is null || !compilationContext.Options.IsEnabled(reference.SyntaxTree))
                {
                    continue;
                }

                symbolContext.ReportDiagnostic(Diagnostic.Create(
                    ExceptionableDiagnostics.UnusedDeclaredException,
                    reference.GetSyntax().GetLocation(),
                    declared.GetExceptionDisplayName(),
                    describe(owner)));
            }
        }

        private static bool tryGetDeclaredException(
            AttributeData attribute,
            ExceptionableCompilationContext compilationContext,
            out INamedTypeSymbol? exceptionType)
        {
            if (ThrowsAttributeReader.TryGetThrowsType(attribute, out ITypeSymbol? typeArgument) &&
                typeArgument is INamedTypeSymbol named &&
                compilationContext.ExceptionTypeResolver.IsExceptionType(named))
            {
                exceptionType = named;
                return true;
            }

            if (ThrowsAttributeReader.TryGetThrowsName(attribute, out string name))
            {
                exceptionType = compilationContext.ExceptionTypeResolver.ResolveExceptionType(name);
                return exceptionType is not null;
            }

            exceptionType = null;
            return false;
        }

        private static string describe(ISymbol owner)
        {
            return owner switch
            {
                IMethodSymbol method => method.GetContractDisplayName(),
                _ => owner.Name,
            };
        }
    }
}