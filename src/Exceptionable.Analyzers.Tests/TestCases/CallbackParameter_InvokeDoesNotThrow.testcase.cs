// Tests invoking a [DoesNotThrow] callback inside a [DoesNotThrow] method.
// Expected behavior: the invocation is not EXC003.
using System;
using Exceptionable;
class C
{
    [DoesNotThrow]
    void Run([DoesNotThrow] Action callback)
    {
        callback();
    }
}
