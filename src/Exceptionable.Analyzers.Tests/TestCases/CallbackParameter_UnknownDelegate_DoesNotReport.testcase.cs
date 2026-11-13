// Tests a delegate whose body is not visible.
// Expected behavior: do not report EXC010, because the callback's exceptions are unknown.
using System;
using Exceptionable;
class C
{
    void Register([DoesNotThrow] Action callback) { }
    void M(Action callback) { Register(callback); }
}
