// Tests rethrowing a caught exception from a DoesNotThrow method.
// Expected behavior: report EXC001 for the rethrown exception.
using Exceptionable;
class C {
    [DoesNotThrow] void M() { try { } catch (System.Exception) { throw; } }
}
