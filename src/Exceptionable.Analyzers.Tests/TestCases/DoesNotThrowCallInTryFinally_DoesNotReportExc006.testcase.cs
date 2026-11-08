// Tests a try/finally with no catch around a DoesNotThrow call.
// Expected behavior: do not report EXC006. There is no catch clause to remove.
using Exceptionable;
class C {
    [DoesNotThrow] void Safe() { }
    void Caller() { try { Safe(); } finally { } }
}
