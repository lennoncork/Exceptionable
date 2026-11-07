// Tests invoking a constructor annotated with a Throws contract.
// Expected behavior: report EXC003 at the object creation.
using Exceptionable;
class Resource {
    [Throws("System.IO.IOException")] public Resource() { }
}
class C { void Caller() { _ = new Resource(); } }
