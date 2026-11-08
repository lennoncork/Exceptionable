// Tests resolving an unqualified exception type name used in a Throws contract.
// Expected behavior: resolve IOException and report neither EXC004 nor EXC003.
using Exceptionable;
class C {
    [Throws("IOException")] void M() { }
    [Throws("System.Exception")] void Caller() { M(); }
}
