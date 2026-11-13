using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
using Exceptionable.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Exceptionable.Analyzers.Analysis
{
    internal sealed class ContractDeclarationAnalysis : ISymbolRule
    {
        public void Analyze(
            SymbolAnalysisContext symbolContext,
            ExceptionableCompilationContext compilationContext)
        {
            if (symbolContext.Symbol is not IMethodSymbol &&
                symbolContext.Symbol is not INamedTypeSymbol &&
                symbolContext.Symbol is not IPropertySymbol &&
                symbolContext.Symbol is not IEventSymbol &&
                symbolContext.Symbol is not IParameterSymbol)
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
                    symbolContext.ReportDiagnostic(DiagnosticFactory.ConflictingContracts(
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

                if (ThrowsAttributeReader.TryGetThrowsType(attribute, out ITypeSymbol? typeArgument))
                {
                    if (typeArgument is not null &&
                        compilationContext.ExceptionTypeResolver.IsExceptionType(typeArgument))
                    {
                        continue;
                    }

                    reportInvalidThrows(
                        symbolContext,
                        attributeReference,
                        typeArgument?.ToDisplayString() ?? string.Empty,
                        typeArgument is null || typeArgument.TypeKind == TypeKind.Error
                            ? "is not a valid exception type in this compilation"
                            : "does not derive from System.Exception");
                    continue;
                }

                if (!ThrowsAttributeReader.TryGetThrowsName(attribute, out string? throwsName))
                {
                    reportInvalidThrows(
                        symbolContext,
                        attributeReference,
                        string.Empty,
                        "is not a valid exception type in this compilation");
                    continue;
                }

                ExceptionTypeResolution resolution = compilationContext.ExceptionTypeResolver.Resolve(throwsName);
                if (resolution.Type is not null)
                {
                    continue;
                }

                reportInvalidThrows(
                    symbolContext,
                    attributeReference,
                    throwsName,
                    describeInvalidName(resolution));
            }

            if (symbol is IParameterSymbol parameter &&
                (hasDoesNotThrow || throwsAttributes.Length > 0) &&
                !CallbackTargets.IsDelegateType(parameter.Type))
            {
                Location? location = parameter.Locations.FirstOrDefault(candidate =>
                    candidate.IsInSource &&
                    candidate.SourceTree is not null &&
                    compilationContext.Options.IsEnabled(candidate.SourceTree));
                if (location is not null)
                {
                    symbolContext.ReportDiagnostic(DiagnosticFactory.CallbackContractTarget(
                        location,
                        parameter.Name));
                }
            }
        }

        private static void reportInvalidThrows(
            SymbolAnalysisContext symbolContext,
            SyntaxReference attributeReference,
            string typeName,
            string reason)
        {
            symbolContext.ReportDiagnostic(DiagnosticFactory.ThrowsUsage(
                attributeReference.GetSyntax().GetLocation(),
                typeName,
                reason));
        }

        private static string describeInvalidName(ExceptionTypeResolution resolution)
        {
            if (resolution.IsAmbiguous)
            {
                return "is ambiguous";
            }

            return resolution.IsNotAnException
                ? "does not derive from System.Exception"
                : "is not a valid exception type in this compilation";
        }
    }
}