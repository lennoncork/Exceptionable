// Tests a method marked DoesNotThrow that throws an exception.
// Expected behavior: report EXC001 for the escaping exception.
using Exceptionable;
class C { [DoesNotThrow] void M() { throw new System.InvalidOperationException(); } }
