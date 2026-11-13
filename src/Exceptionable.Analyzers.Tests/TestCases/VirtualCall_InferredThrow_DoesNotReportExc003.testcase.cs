// Tests an open virtual call whose base body throws without a declared contract.
// Expected behavior: do not report EXC003, because an override can replace that throw.
using Exceptionable;
class Base
{
    public virtual void M()
    {
        throw new System.IO.IOException();
    }
}
class Caller
{
    [DoesNotThrow]
    void Call(Base item)
    {
        item.M();
    }
}
