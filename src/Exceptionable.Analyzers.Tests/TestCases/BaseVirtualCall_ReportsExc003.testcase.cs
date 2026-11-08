// Tests a base call to a virtual method. The receiver is statically bound.
// Expected behavior: report EXC003 for the base call and not EXC001.
using Exceptionable;
class Base
{
    [Throws("System.IO.IOException")]
    public virtual void M()
    {
        throw new System.IO.IOException();
    }
}
class Derived : Base
{
    [DoesNotThrow]
    public override void M()
    {
        base.M();
    }
}
