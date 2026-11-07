// Tests a throw statement that is unreachable after a return.
// Expected behavior: do not report EXC001 for the unreachable throw.
using Exceptionable;
class C {
    [DoesNotThrow] void M() {
        return;
        throw new System.Exception();
    }
}
