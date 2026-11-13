// Tests non-async methods that throw before returning a task and declare [DoesNotThrow].
// Expected behavior: report EXC001 on each throw. Callers see that explicit contract, so the calls are not EXC003.
using Exceptionable;
using System.Threading.Tasks;

class C
{
    [DoesNotThrow]
    Task ImmediateTaskFailure()
    {
        throw new System.InvalidOperationException();
    }

    [DoesNotThrow]
    ValueTask ImmediateValueTaskFailure()
    {
        throw new System.NotSupportedException();
    }

    [DoesNotThrow]
    void Caller()
    {
        _ = ImmediateTaskFailure();
        _ = ImmediateValueTaskFailure();
    }
}


