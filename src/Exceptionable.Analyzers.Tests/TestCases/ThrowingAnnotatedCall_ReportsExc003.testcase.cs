// Tests calling a method with an unhandled Throws contract.
// Expected behavior: report EXC003 at the call site.
using Exceptionable;
class C {
    [Throws("System.InvalidOperationException")] void M() { }
    void Caller() { M(); }
}
