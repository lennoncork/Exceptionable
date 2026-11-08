// Tests a base call whose method is virtual and [DoesNotThrow].
// Expected behavior: report EXC006 because the base call is statically bound.
using Exceptionable;
class Base
{
    [DoesNotThrow]
    public virtual void Safe()
    {
    }
}
class Derived : Base
{
    void M()
    {
        try
        {
            base.Safe();
        }
        catch
        {
        }
    }
}
