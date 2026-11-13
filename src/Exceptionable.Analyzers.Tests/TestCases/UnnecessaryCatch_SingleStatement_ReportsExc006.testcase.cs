// Tests a catch around a call that declares it does not throw.
// Expected behavior: report EXC006, and the fix removes the try.
using Exceptionable;
class C
{
    [DoesNotThrow]
    void Safe() { }
    void M()
    {
        try { Safe(); }
        catch (System.Exception) { }
    }
}
