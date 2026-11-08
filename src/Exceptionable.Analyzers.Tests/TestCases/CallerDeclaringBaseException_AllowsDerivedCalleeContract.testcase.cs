// Tests whether a caller's base exception contract covers a derived callee exception.
// Expected behavior: do not report EXC003 because System.Exception covers IOException.
using Exceptionable;
class C {
    [Throws("System.IO.IOException")] void M() { }
    [Throws("System.Exception")] void Caller() { M(); }
}
