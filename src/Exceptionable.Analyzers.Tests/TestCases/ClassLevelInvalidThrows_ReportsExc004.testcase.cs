// Tests a class-level Throws contract that refers to a missing type.
// Expected behavior: report EXC004 for the unresolved contract type.
using Exceptionable;
[Throws("MissingException")] class C { void M() { } }
