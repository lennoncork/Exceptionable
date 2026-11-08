// Tests an open virtual call. The runtime override is not known.
// Expected behavior: do not report EXC003 at the call.
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
