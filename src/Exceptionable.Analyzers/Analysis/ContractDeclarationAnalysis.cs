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
            if (symbolContext.Symbol is not IMethodSymbol &&
                symbolContext.Symbol is not INamedTypeSymbol &&
                symbolContext.Symbol is not IPropertySymbol &&
                symbolContext.Symbol is not IEventSymbol)
            {
                return;
            }

            ISymbol symbol = symbolContext.Symbol;
            if (!symbol.DeclaringSyntaxReferences.Any())
            {
                return;
            }

            ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
            bool hasDoesNotThrow = attributes.Any(ThrowsAttributeReader.IsDoesNotThrow);
            var throwsAttributes = attributes.Where(ThrowsAttributeReader.IsThrows).ToImmutableArray();

            if (hasDoesNotThrow && throwsAttributes.Length > 0)
            {
                SyntaxReference? attributeReference = throwsAttributes
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

            foreach (AttributeData? attribute in throwsAttributes)
            {
                SyntaxReference? attributeReference = attribute.ApplicationSyntaxReference;
                if (attributeReference is null ||
                    !compilationContext.Options.IsEnabled(attributeReference.SyntaxTree))
                {
                    continue;
                }

                if (!ThrowsAttributeReader.TryGetThrowsName(attribute, out string? throwsName))
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(
                        ExceptionableDiagnostics.ThrowsUsage,
                        attributeReference.GetSyntax().GetLocation(),
                        string.Empty,
                        "is not a valid exception type in this compilation"));
                    continue;
                }

                ExceptionTypeResolution resolution = compilationContext.ExceptionTypeResolver.Resolve(throwsName);
                if (resolution.Type is not null)
                {
                    continue;
                }

                symbolContext.ReportDiagnostic(Diagnostic.Create(
                    ExceptionableDiagnostics.ThrowsUsage,
                    attributeReference.GetSyntax().GetLocation(),
                    throwsName,
                    resolution.IsAmbiguous
                        ? "is ambiguous"
                        : "is not a valid exception type in this compilation"));
            }
        }
    }
}