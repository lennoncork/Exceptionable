// Tests a method-level Throws contract overriding its class-level contract.
// Expected behavior: report EXC002 for the IOException not allowed by the method contract.
using Exceptionable;
[Throws("System.IO.IOException")]
class C {
    [Throws("System.InvalidOperationException")]
    void M() { throw new System.IO.IOException(); }
}
