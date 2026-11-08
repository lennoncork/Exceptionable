// Verifies Synchronous Task Returning Methods Analyze Immediate Throws But Calls Remain Deferred.
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


