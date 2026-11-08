// Tests a Throws contract that names a type that does not derive from Exception.
// Expected behavior: report EXC004 for the invalid contract type.
using Exceptionable;
class C { [Throws("System.String")] void M() { } }
