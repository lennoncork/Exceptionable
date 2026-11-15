// An override does not inherit a base method [DoesNotThrow] contract.
// Expected behavior: EXC009 on each override that does not declare it. The sealed call is not EXC006.
using Exceptionable;
class Base
{
    [DoesNotThrow]
    public virtual void Safe() { }
}
sealed class Derived : Base
{
    public override void Safe() { }
}
class Mid : Base
{
    [DoesNotThrow]
    public override void Safe() { }
}
class Further : Mid
{
    public override void Safe() { }
}
class Caller
{
    void Call(Derived item) { try { item.Safe(); } catch (System.Exception) { } }
}
