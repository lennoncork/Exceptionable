// Tests a single try/catch around a call to a DoesNotThrow method.
// Expected behavior: report EXC006 because the catch cannot observe a tracked exception.
using Exceptionable;
class C {
    [DoesNotThrow] void Safe() { }
    void Caller() { try { Safe(); } catch (System.Exception) { } }
}
