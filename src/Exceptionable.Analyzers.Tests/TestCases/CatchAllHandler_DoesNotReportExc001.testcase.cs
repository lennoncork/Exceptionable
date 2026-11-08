// Tests that a catch-all handler proves a thrown exception is consumed.
// Expected behavior: do not report EXC001.
using Exceptionable;
class C
{
    [DoesNotThrow]
    void M()
    {
        try
        {
            throw new System.Exception();
        }
        catch
        {
        }
    }
}
