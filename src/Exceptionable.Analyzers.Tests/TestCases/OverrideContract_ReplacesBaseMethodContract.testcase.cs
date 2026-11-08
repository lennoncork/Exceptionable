// Tests an override attribute replacing the base method contract.
// Expected behavior: report EXC002 for IOException and do not report EXC005.
using Exceptionable;
class Base
{
    [Throws("System.IO.IOException")]
    public virtual void M()
    {
    }
}
class Derived : Base
{
    [Throws("System.ArgumentException")]
    public override void M()
    {
        throw new System.IO.IOException();
    }
}
