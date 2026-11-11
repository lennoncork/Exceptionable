using System.Collections.Generic;

namespace Exceptionable.Analyzers.Polyfills
{
    internal sealed class SystemCollectionsGenericPolyfills : PolyfillModule
    {
        internal override string Namespace => "System.Collections.Generic";

        internal override IEnumerable<PolyfillApi> Apis { get; } =
        [
            Api("Dictionary`2", "get_Item", "System.Collections.Generic.KeyNotFoundException"),
            Api("List`1", "get_Item", "System.ArgumentOutOfRangeException"),
            Api("List`1", "set_Item", "System.ArgumentOutOfRangeException"),
            Api("Queue`1", "Dequeue", "System.InvalidOperationException"),
            Api("Queue`1", "Peek", "System.InvalidOperationException"),
            Api("Stack`1", "Pop", "System.InvalidOperationException"),
            Api("Stack`1", "Peek", "System.InvalidOperationException"),
        ];
    }
}