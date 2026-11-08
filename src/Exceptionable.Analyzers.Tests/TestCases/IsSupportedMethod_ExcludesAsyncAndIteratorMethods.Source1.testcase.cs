// Verifies Is Supported Method Excludes Async And Iterator Methods.

using System.Collections.Generic;
using System.Threading.Tasks;
class Base
{
    public virtual void SealedOverride() { }
}
class C : Base
{
    async Task AsyncMethod() { await Task.CompletedTask; }
    async ValueTask AsyncValueTask() { await Task.CompletedTask; }
    IEnumerable<int> IteratorMethod() { yield return 1; }
    async IAsyncEnumerable<int> AsyncIteratorMethod() { await Task.Yield(); yield return 1; }
    void SynchronousMethod() { }
    virtual void VirtualMethod() { }
    void CallsLocalIterator()
    {
        System.Collections.Generic.IEnumerable<int> Local() { yield return 1; }
    }
    public sealed override void SealedOverride() { }
}

