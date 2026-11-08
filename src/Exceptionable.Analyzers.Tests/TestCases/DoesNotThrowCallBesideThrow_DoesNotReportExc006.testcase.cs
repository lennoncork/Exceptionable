// Tests a DoesNotThrow call in the same try as a reachable throw.
// Expected behavior: do not report EXC006. The catch handles the throw.
using Exceptionable;
class C {
    [DoesNotThrow] void Safe() { }
    void Caller() {
        try {
            Safe();
            throw new System.InvalidOperationException();
        } catch (System.Exception) { }
    }
}
