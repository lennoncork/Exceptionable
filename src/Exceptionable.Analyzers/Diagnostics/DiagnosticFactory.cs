using System.Collections.Immutable;
using System.Linq;
using Exceptionable.Analyzers.Contracts;
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
            Diagnostic.Create(
                ExceptionableDiagnostics.ThrowsMismatch,
                location,
                ImmutableDictionary<string, string?>.Empty.Add(ExceptionTypeProperty, exceptionType),
                exceptionType);

        internal static Diagnostic CallbackContractViolation(Location location, string parameterName, string exceptionType) =>
            Diagnostic.Create(
                ExceptionableDiagnostics.CallbackContractViolation,
                location,
                parameterName,
                exceptionType);

        internal static Diagnostic CallbackContractTarget(Location location, string parameterName) =>
            Diagnostic.Create(ExceptionableDiagnostics.CallbackContractTarget, location, parameterName);

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

        internal const string ContractKindProperty = "ContractKind";

        internal const string ExceptionTypesProperty = "ExceptionTypes";

        internal const string DoesNotThrowContract = "DoesNotThrow";

        internal const string ThrowsContract = "Throws";

        internal static Diagnostic InterfaceContractMismatch(
            Location location,
            string member,
            string interfaceMember,
            ExceptionContract? contractToDeclare)
        {
            ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty;
            if (contractToDeclare is ExceptionContract contract && contract.HasContract)
            {
                properties = properties
                    .Add(ContractKindProperty, contract.IsDoesNotThrow ? DoesNotThrowContract : ThrowsContract)
                    .Add(
                        ExceptionTypesProperty,
                        string.Join("|", contract.DeclaredTypes.Select(type => type.GetExceptionDisplayName())));
            }

            return Diagnostic.Create(
                ExceptionableDiagnostics.InterfaceContractMismatch,
                location,
                properties,
                member,
                interfaceMember);
        }
    }
}