// Tests whether a class-level exception contract applies to its instance method calls.
// Expected behavior: report EXC003 when calling the method without handling IOException.
using Exceptionable;
[Throws("System.IO.IOException")] class Base { public void M() { } }
class Caller { void Call(Base instance) { instance.M(); } }
