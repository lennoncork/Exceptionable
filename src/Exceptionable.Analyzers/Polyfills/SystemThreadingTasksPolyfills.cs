using System.Collections.Generic;

namespace Exceptionable.Analyzers.Polyfills
{
    // Blocking members surface task faults as AggregateException. Awaited tasks are not modeled yet.
    internal sealed class SystemThreadingTasksPolyfills : PolyfillModule
    {
        internal override string Namespace => "System.Threading.Tasks";

        internal override IEnumerable<PolyfillApi> Apis { get; } =
        [
            Api("Task", "Wait", "System.AggregateException"),
            Api("Task", "WaitAll", "System.AggregateException"),
            Api("Task`1", "get_Result", "System.AggregateException"),
        ];
    }
}