// Tests a method whose thrown exception is not covered by its Throws contract.
// Expected behavior: report EXC002 for the contract mismatch.
using Exceptionable;
class C { [Throws("System.IO.IOException")] void M() { throw new System.InvalidOperationException(); } }
