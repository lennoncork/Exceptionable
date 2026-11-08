// Tests a short exception name that collides with a non-exception type.
// Expected behavior: resolve System.IO.IOException and do not report EXC004 or EXC002.
using Exceptionable;
class IOException
{
}
class C
{
    [Throws("IOException")]
    void M()
    {
        throw new System.IO.IOException();
    }
}
