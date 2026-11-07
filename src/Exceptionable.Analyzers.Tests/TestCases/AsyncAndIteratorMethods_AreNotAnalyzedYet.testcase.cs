// Tests throws inside async and iterator methods, which are currently unsupported.
// Expected behavior: do not report EXC001 for either method.
using Exceptionable;
using System.Collections.Generic;
class C {
    [DoesNotThrow] async System.Threading.Tasks.Task AsyncMethod() {
        await System.Threading.Tasks.Task.Yield();
        throw new System.Exception();
    }
    [DoesNotThrow] IEnumerable<int> Iterator() {
        yield return 1;
        throw new System.Exception();
    }
}
