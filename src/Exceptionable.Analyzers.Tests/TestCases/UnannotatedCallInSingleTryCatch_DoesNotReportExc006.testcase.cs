// Tests a single try/catch around a call with no exception contract.
// Expected behavior: do not report EXC006. An unannotated call is not proven non-throwing.
class C {
    void Unknown() { }
    void Caller() { try { Unknown(); } catch (System.Exception) { } }
}
