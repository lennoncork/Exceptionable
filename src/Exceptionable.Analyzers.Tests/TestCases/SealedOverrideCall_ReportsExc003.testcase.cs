// Tests a sealed override called through its sealed declaring type.
// Expected behavior: report EXC003. The implementation is fixed at the call.
using Exceptionable;
class Base
{
    public virtual void M()
    {
    }
}
sealed class Derived : Base
{
    [Throws("System.IO.IOException")]
    public sealed override void M()
    {
        throw new System.IO.IOException();
    }
}
class Caller
{
    void Call(Derived item)
    {
        item.M();
    }
}
