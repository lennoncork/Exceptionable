// Tests that analysis remains disabled when neither project nor file enables it.
// Expected behavior: no analyzer diagnostics are reported.
using Exceptionable;
class C { [DoesNotThrow] void M() { throw new System.Exception(); } }
