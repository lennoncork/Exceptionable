using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    internal static class ContractDeclarationAnalysis
    {
        internal static void Analyze(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext)
        {
            if (symbolContext.Symbol is not IMethodSymbol && symbolContext.Symbol is not INamedTypeSymbol)
            {
                return;
            }

            var symbol = symbolContext.Symbol;
            if (!symbol.DeclaringSyntaxReferences.Any())
            {
                return;
            }

            var attributes = symbol.GetAttributes();
            var hasDoesNotThrow = attributes.Any(ThrowsAttributeReader.IsDoesNotThrow);
            var throwsAttributes = attributes.Where(ThrowsAttributeReader.IsThrows).ToImmutableArray();

            if (hasDoesNotThrow && throwsAttributes.Length > 0)
            {
                var attributeReference = throwsAttributes
                    .Select(attribute => attribute.ApplicationSyntaxReference)
                    .FirstOrDefault(reference => reference is not null &&
                        compilationContext.Options.IsEnabled(reference.SyntaxTree));
                if (attributeReference is not null)
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(
                        ExceptionableDiagnostics.ConflictingContracts,
                        attributeReference.GetSyntax().GetLocation()));
                }
            }

            foreach (var attribute in throwsAttributes)
            {
                var attributeReference = attribute.ApplicationSyntaxReference;
                if (attributeReference is null ||
                    !compilationContext.Options.IsEnabled(attributeReference.SyntaxTree))
                {
                    continue;
                }

                if (!ThrowsAttributeReader.TryGetThrowsName(attribute, out var throwsName) ||
                    compilationContext.ExceptionTypeResolver.ResolveExceptionType(throwsName) is null)
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(
                        ExceptionableDiagnostics.ThrowsUsage,
                        attributeReference.GetSyntax().GetLocation(),
                        ThrowsAttributeReader.GetThrowsName(attribute)));
                }
            }
        }
    }
}