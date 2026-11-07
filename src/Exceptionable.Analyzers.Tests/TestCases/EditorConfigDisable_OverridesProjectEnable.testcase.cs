// Tests a file-level disable setting when project-level analysis is enabled.
// Expected behavior: the file setting wins and no analyzer diagnostics are reported.
using Exceptionable;
class C { [DoesNotThrow] void M() { throw new System.Exception(); } }
