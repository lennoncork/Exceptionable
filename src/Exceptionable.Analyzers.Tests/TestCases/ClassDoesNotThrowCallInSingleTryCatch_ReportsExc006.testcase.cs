// Tests a single try/catch around a call covered by a class-level DoesNotThrow contract.
// Expected behavior: report EXC006 for the effective no-throw contract.
using Exceptionable;
[DoesNotThrow]
class C {
    void Safe() { }
    void Caller() { try { Safe(); } catch (System.Exception) { } }
}
