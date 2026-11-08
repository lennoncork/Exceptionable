// Tests a single try/catch around a call that declares a thrown exception.
// Expected behavior: do not report EXC006 or EXC003. The catch handles IOException.
using Exceptionable;
class C {
    [Throws("System.IO.IOException")] void M() { }
    void Caller() { try { M(); } catch (System.Exception) { } }
}
