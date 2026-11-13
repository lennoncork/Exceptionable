// Tests a non-async method that throws before returning a task.
// Expected behavior: report EXC003 at the call. The throw is synchronous.
using System.Threading.Tasks;
using Exceptionable;
class C
{
    Task Immediate()
    {
        throw new System.InvalidOperationException();
    }

    [DoesNotThrow]
    void Caller()
    {
        _ = Immediate();
    }
}
