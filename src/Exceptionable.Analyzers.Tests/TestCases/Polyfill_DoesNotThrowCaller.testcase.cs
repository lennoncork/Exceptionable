// A [DoesNotThrow] method calls polyfilled APIs from three namespaces.
// Expected behavior: with polyfills enabled, EXC003 reports First, the dictionary indexer, and
// Task.Result. FirstOrDefault and the seeded Aggregate overload are not reported.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exceptionable;
class C {
    [DoesNotThrow]
    void M(List<int> items, Dictionary<string, int> map, Task<int> task) {
        int first = items.First();
        int value = map["key"];
        int result = task.Result;
        int safe = items.FirstOrDefault();
        int seeded = items.Aggregate(0, (a, b) => a + b);
    }
}
