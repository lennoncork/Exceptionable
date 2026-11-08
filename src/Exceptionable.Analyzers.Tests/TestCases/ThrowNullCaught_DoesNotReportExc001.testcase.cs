// Tests a caught throw null. NullReferenceException is handled by the catch.
// Expected behavior: do not report EXC001.
using Exceptionable;
class C
{
    [DoesNotThrow]
    void M()
    {
        try
        {
            throw null;
        }
        catch (System.NullReferenceException)
        {
        }
    }
}
