// Tests a throwing call in a catch block belonging to a try with the same exception type.
// Expected behavior: report EXC003 because the try's catch does not handle calls in its catch.
using Exceptionable;
class C {
    [Throws("System.Exception")] void M() { }
    void Caller() {
        try { } catch (System.Exception) { M(); }
    }
}
