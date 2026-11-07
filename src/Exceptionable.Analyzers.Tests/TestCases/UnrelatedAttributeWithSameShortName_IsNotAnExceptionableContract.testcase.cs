// Tests a different namespace's Throws attribute with the same short name.
// Expected behavior: treat it as unrelated and report neither EXC003 nor EXC004.
namespace Other {
    class ThrowsAttribute : System.Attribute {
        public ThrowsAttribute(string name) { }
    }
}
class C {
    [Other.Throws("System.IO.IOException")] void M() { }
    void Caller() { M(); }
}
