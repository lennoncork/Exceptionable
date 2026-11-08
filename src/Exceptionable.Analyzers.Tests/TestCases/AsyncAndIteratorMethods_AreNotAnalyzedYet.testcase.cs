// Tests async and iterator bodies and their call sites, which are currently unsupported.
// Expected behavior: do not report EXC001, EXC002, or EXC003 for these methods.
using Exceptionable;
using System.Collections.Generic;
using System.Threading.Tasks;
class C {
    [Throws("System.Exception")] async Task AsyncMethod() {
        await Task.Yield();
        throw new System.Exception();
    }
    [Throws("System.Exception")] IEnumerable<int> Iterator() {
        yield return 1;
        throw new System.Exception();
    }
    [DoesNotThrow] void CallAsync() {
        _ = AsyncMethod();
    }
    [DoesNotThrow] void Enumerate() {
        foreach (var value in Iterator()) {
        }
    }
}
