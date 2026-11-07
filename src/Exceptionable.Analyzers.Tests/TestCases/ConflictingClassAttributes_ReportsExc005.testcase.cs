// Tests conflicting DoesNotThrow and Throws attributes on one class.
// Expected behavior: report EXC005 for the conflicting contracts.
using Exceptionable;
[DoesNotThrow] [Throws("System.Exception")] class C { }
