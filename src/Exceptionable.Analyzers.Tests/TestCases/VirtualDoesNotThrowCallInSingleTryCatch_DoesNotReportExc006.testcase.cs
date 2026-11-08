// Tests a single try/catch around an open virtual DoesNotThrow call.
// Expected behavior: do not report EXC006. An override may still throw.
using Exceptionable;
class Base {
    [DoesNotThrow] public virtual void Safe() { }
}
class Caller {
    void Call(Base item) { try { item.Safe(); } catch (System.Exception) { } }
}
