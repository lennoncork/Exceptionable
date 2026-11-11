namespace Exceptionable.Analyzers.CodeFixes
{
    // The code fixes do not reference the analyzer assembly, so the IDs and property keys it publishes are repeated here.
    internal static class ExceptionableDiagnosticIds
    {
        internal const string CallerViolation = "EXC003";

        internal const string UnusedDeclaredException = "EXC008";

        /// <summary>The diagnostic property that holds the namespace-qualified exception type name.</summary>
        internal const string ExceptionTypeProperty = "ExceptionType";
    }
}