// Tests whether a caller's derived exception contract covers a base callee exception.
// Expected behavior: report EXC003 because IOException does not cover System.Exception.
using Exceptionable;
class C {
    [Throws("System.Exception")] void M() { }
    [Throws("System.IO.IOException")] void Caller() { M(); }
}
