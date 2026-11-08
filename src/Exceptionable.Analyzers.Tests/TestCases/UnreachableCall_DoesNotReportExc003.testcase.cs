// Tests a throwing call that is unreachable after a return.
// Expected behavior: do not report EXC003 for the unreachable call.
using Exceptionable;
class C {
    [Throws("System.Exception")] void M() { }
    void Caller() {
        return;
        M();
    }
}
