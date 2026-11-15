// An override that declares [DoesNotThrow] fulfills the base method.
// Expected behavior: no EXC009. The sealed call is EXC006.
using Exceptionable;
class Base
{
    [DoesNotThrow]
    public virtual void Safe() { }
}
sealed class Derived : Base
{
    [DoesNotThrow]
    public override void Safe() { }
}
class Caller
{
    void Call(Derived item) { try { item.Safe(); } catch (System.Exception) { } }
}
