// Tests throwing the exact exception type declared by [Throws].
// Expected behavior: do not report EXC002.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    void M()
    {
        throw new System.IO.IOException();
    }
}
