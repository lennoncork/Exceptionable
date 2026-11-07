// A compound property assignment invokes both its getter and setter.
// Expected behavior: report EXC003 for each accessor contract.
using Exceptionable;
class C {
    public int Value {
        [Throws("System.IO.IOException")] get { return 0; }
        [Throws("System.InvalidOperationException")] set { }
    }
    void Caller() { Value += 1; }
}