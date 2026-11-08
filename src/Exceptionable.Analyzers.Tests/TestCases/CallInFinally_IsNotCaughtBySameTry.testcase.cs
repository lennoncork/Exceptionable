// Tests a throwing call in finally against a catch on the same try statement.
// Expected behavior: report EXC003 because that catch cannot handle calls in finally.
using Exceptionable;
class C {
    [Throws("System.Exception")] void M() { }
    void Caller() {
        try { } catch (System.Exception) { } finally { M(); }
    }
}
