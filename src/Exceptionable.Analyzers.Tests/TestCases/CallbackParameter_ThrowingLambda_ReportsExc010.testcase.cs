// Tests a [DoesNotThrow] delegate parameter.
// Expected behavior: report EXC010 when the lambda throws.
using System;
using Exceptionable;
class C
{
    void Register([DoesNotThrow] Action callback) { }
    void M()
    {
        Register(() => throw new InvalidOperationException());
    }
}
