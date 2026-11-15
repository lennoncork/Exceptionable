using System.Collections.Generic;

namespace Exceptionable.Analyzers.Polyfills
{
    internal sealed class SystemPolyfills : PolyfillModule
    {
        internal override string Namespace => "System";

        internal override IEnumerable<PolyfillApi> Apis { get; } =
        [
            Api("Int32", "Parse", "System.FormatException", "System.OverflowException"),
            Api("Int64", "Parse", "System.FormatException", "System.OverflowException"),
            Api("Guid", "Parse", "System.FormatException"),
            Api("DateTime", "Parse", "System.FormatException"),
            // Parameterless Object members only. Format overloads throw FormatException.
            // Static Object.Equals(object, object) forwards to the instance method and is omitted.
            // Non-generic IComparable.CompareTo(object) throws ArgumentException and is omitted.
            DoesNotThrow("Object", "ToString", 0),
            DoesNotThrow("Object", "Equals", 1),
            DoesNotThrow("Object", "GetHashCode", 0),
            DoesNotThrow("Object", "ReferenceEquals", 2),
            DoesNotThrow("IEquatable`1", "Equals", 1),
            DoesNotThrow("IComparable`1", "CompareTo", 1),
        ];
    }
}