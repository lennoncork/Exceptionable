// Tests a catch filter whose truth cannot be established at compile time.
// Expected behavior: report EXC003 because the filtered catch does not prove handling.
using Exceptionable;
class C {
    [Throws("System.IO.IOException")] void M() { }
    bool Filter() => true;
    void Caller() { try { M(); } catch (System.Exception) when (Filter()) { } }
}
