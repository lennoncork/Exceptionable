namespace Exceptionable.Analyzers.CodeFixes
{
    // The code fixes do not reference the analyzer assembly, so the IDs and property keys it publishes are repeated here.
    internal static class ExceptionableDiagnosticIds
    {
        internal const string ThrowsMismatch = "EXC002";

        internal const string CallerViolation = "EXC003";

        internal const string UnnecessaryTryCatch = "EXC006";

        internal const string UnusedDeclaredException = "EXC008";

        internal const string InterfaceContractMismatch = "EXC009";

        /// <summary>The diagnostic property that holds the namespace-qualified exception type name.</summary>
        internal const string ExceptionTypeProperty = "ExceptionType";

        internal const string ContractKindProperty = "ContractKind";

        internal const string ExceptionTypesProperty = "ExceptionTypes";

        internal const string DoesNotThrowContract = "DoesNotThrow";

        internal const string ThrowsContract = "Throws";
    }
}