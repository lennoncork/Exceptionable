// Tests a throwing call handled by a catch for a base exception type.
// Expected behavior: do not report EXC003 because System.Exception catches IOException.
using Exceptionable;
class C {
    [Throws("System.IO.IOException")] void M() { }
    void Caller() { try { M(); } catch (System.Exception) { } }
}
