// Tests a lambda stored in a local and then invoked.
// Expected behavior: report EXC003 at the invocation. The throw is not EXC001 on the outer method.
using System;
using Exceptionable;
class C
{
    [DoesNotThrow]
    void M()
    {
        Action action = () => throw new InvalidOperationException();
        action();
    }
}
