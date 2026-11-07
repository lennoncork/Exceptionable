// Tests recognition of a source-defined Exceptionable.ThrowsAttribute without the package.
// Expected behavior: report EXC003 at the call to the annotated method.
namespace Exceptionable {
    [System.AttributeUsage(System.AttributeTargets.Method)]
    sealed class ThrowsAttribute : System.Attribute {
        public ThrowsAttribute(string name) { }
    }
}
class C {
    [Exceptionable.Throws("System.IO.IOException")] void M() { }
    void Caller() { M(); }
}
