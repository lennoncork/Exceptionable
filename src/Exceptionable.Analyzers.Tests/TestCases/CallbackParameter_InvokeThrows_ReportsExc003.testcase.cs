// Tests invoking a callback that is allowed to throw IOException.
// Expected behavior: report EXC003 when the caller does not allow that exception.
using System;
using Exceptionable;
class C
{
    [DoesNotThrow]
    void Run([Throws("System.IO.IOException")] Action callback)
    {
        callback();
    }
}
