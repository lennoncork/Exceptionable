// Tests an open virtual call. The runtime override may throw more, but the
// statically selected method publishes [Throws].
// Expected behavior: report EXC003 for IOException at the call.
using Exceptionable;
class Base
{
    [Throws("System.IO.IOException")]
    public virtual void M()
    {
        throw new System.IO.IOException();
    }
}
class Caller
{
    void Call(Base item)
    {
        item.M();
    }
}
