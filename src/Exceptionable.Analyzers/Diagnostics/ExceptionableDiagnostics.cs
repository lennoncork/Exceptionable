using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Diagnostics
{
    internal static class ExceptionableDiagnostics
    {
        internal static readonly DiagnosticDescriptor DoesNotThrowEscapes = new(
            "EXC001",
            "Exception escapes a [DoesNotThrow] contract",
            "Exception type '{0}' escapes a method marked [DoesNotThrow]",
            "Exceptionable",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC001.md");

        internal static readonly DiagnosticDescriptor ThrowsMismatch = new(
            "EXC002",
            "Exception escapes a declared contract",
            "Exception type '{0}' escapes a method but is not declared by its contract",
            "Exceptionable",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC002.md");

        internal static readonly DiagnosticDescriptor CallerViolation = new(
            "EXC003",
            "Call to annotated method may throw an unhandled exception",
            "Call to '{0}' may throw '{1}' which is neither caught nor permitted by the caller's contract",
            "Exceptionable",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC003.md");

        internal static readonly DiagnosticDescriptor ThrowsUsage = new(
            "EXC004",
            "Invalid Throws attribute name",
            "The type name '{0}' in [Throws] {1}",
            "Exceptionable",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC004.md");

        internal static readonly DiagnosticDescriptor ConflictingContracts = new(
            "EXC005",
            "Conflicting exception attributes",
            "A member cannot be annotated with both [DoesNotThrow] and [Throws]",
            "Exceptionable",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC005.md");

        internal static readonly DiagnosticDescriptor UnnecessaryTryCatch = new(
            "EXC006",
            "Try/catch is unnecessary for a [DoesNotThrow] call",
            "Try/catch is unnecessary because {0}",
            "Exceptionable",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/lennoncork/Exceptionable/blob/main/docs/diagnostics/EXC006.md");

        internal static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                DoesNotThrowEscapes,
                ThrowsMismatch,
                CallerViolation,
                ThrowsUsage,
                ConflictingContracts,
                UnnecessaryTryCatch);
    }
}