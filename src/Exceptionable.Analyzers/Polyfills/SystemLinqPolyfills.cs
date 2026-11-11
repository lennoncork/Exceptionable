using System.Collections.Generic;

namespace Exceptionable.Analyzers.Polyfills
{
    // Only operators that execute immediately are listed. Deferred operators such as Select and
    // Where return an enumerable and are skipped by the deferred-return policy.
    internal sealed class SystemLinqPolyfills : PolyfillModule
    {
        internal override string Namespace => "System.Linq";

        internal override IEnumerable<PolyfillApi> Apis { get; } =
        [
            Api("Enumerable", "First", "System.InvalidOperationException"),
            Api("Enumerable", "Last", "System.InvalidOperationException"),
            Api("Enumerable", "Single", "System.InvalidOperationException"),
            Api("Enumerable", "ElementAt", "System.ArgumentOutOfRangeException"),
            // Only the overload without a seed fails on an empty sequence.
            Api("Enumerable", "Aggregate", 2, "System.InvalidOperationException"),
        ];
    }
}