// Tests reading and assigning through separately annotated property accessors.
// Expected behavior: report two EXC003 diagnostics, one for each accessor call.
using Exceptionable;
class C {
    public int Value {
        [Throws("System.IO.IOException")] get { return 0; }
        [Throws("System.InvalidOperationException")] set { }
    }
    void Caller() { _ = Value; Value = 1; }
}
