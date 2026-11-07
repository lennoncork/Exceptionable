// Tests a throw in finally against a catch on the same try statement.
// Expected behavior: report EXC001 because that catch cannot handle the finally throw.
using Exceptionable;
class C {
    [DoesNotThrow] void M() {
        try { } catch (System.Exception) { } finally { throw new System.Exception(); }
    }
}
