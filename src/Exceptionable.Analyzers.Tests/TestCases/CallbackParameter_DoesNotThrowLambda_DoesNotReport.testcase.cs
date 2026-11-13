// Tests a callback that catches its own exception.
// Expected behavior: do not report EXC010.
using System;
using Exceptionable;
class C
{
    void Register([DoesNotThrow] Action callback) { }
    void M()
    {
        Register(() =>
        {
            try { throw new InvalidOperationException(); }
            catch (InvalidOperationException) { }
        });
    }
}
