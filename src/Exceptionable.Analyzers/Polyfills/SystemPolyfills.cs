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
        ];
    }
}