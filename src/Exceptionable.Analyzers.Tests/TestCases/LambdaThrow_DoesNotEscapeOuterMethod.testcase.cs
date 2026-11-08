// Tests a throw inside a lambda. Creating or invoking the delegate is not that throw.
// Expected behavior: do not report EXC001 for the lambda body.
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
