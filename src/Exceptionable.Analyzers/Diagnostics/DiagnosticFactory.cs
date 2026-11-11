using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Exceptionable.Analyzers.Diagnostics
{
    /// <summary>Creates diagnostics so each rule's message arguments and properties are defined in one place.</summary>
    internal static class DiagnosticFactory
    {
        /// <summary>Property key read by the code fix providers; mirrors <c>ExceptionableDiagnosticIds.ExceptionTypeProperty</c>.</summary>
        internal const string ExceptionTypeProperty = "ExceptionType";

        internal static Diagnostic DoesNotThrowEscapes(Location location, string exceptionType) =>
            Diagnostic.Create(ExceptionableDiagnostics.DoesNotThrowEscapes, location, exceptionType);

        internal static Diagnostic ThrowsMismatch(Location location, string exceptionType) =>
            Diagnostic.Create(ExceptionableDiagnostics.ThrowsMismatch, location, exceptionType);

        internal static Diagnostic CallerViolation(Location location, string target, string exceptionType) =>
            Diagnostic.Create(
                ExceptionableDiagnostics.CallerViolation,
                location,
                ImmutableDictionary<string, string?>.Empty.Add(ExceptionTypeProperty, exceptionType),
                target,
                exceptionType);

        internal static Diagnostic ThrowsUsage(Location location, string typeName, string reason) =>
            Diagnostic.Create(ExceptionableDiagnostics.ThrowsUsage, location, typeName, reason);

        internal static Diagnostic ConflictingContracts(Location location) =>
            Diagnostic.Create(ExceptionableDiagnostics.ConflictingContracts, location);

        internal static Diagnostic UnnecessaryTryCatch(Location location, string reason) =>
            Diagnostic.Create(ExceptionableDiagnostics.UnnecessaryTryCatch, location, reason);

        internal static Diagnostic UnusedDeclaredException(Location location, string exceptionType, string owner) =>
            Diagnostic.Create(ExceptionableDiagnostics.UnusedDeclaredException, location, exceptionType, owner);

        internal static Diagnostic RedundantArgumentNullException(Location location, string constructor) =>
            Diagnostic.Create(ExceptionableDiagnostics.RedundantArgumentNullException, location, constructor);

        internal static Diagnostic InterfaceContractMismatch(Location location, string member, string interfaceMember) =>
            Diagnostic.Create(ExceptionableDiagnostics.InterfaceContractMismatch, location, member, interfaceMember);
    }
}
