// Tests a Throws contract with a short name made ambiguous by a user-defined type.
// Expected behavior: report EXC004 for the ambiguous exception type.
using Exceptionable;
class IOException : System.Exception { }
class C { [Throws("IOException")] void M() { } }
