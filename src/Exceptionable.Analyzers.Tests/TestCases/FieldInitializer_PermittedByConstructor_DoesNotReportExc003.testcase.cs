// Tests a field initializer whose constructor declares the same exception.
// Expected behavior: do not report EXC003.
using Exceptionable;
class C
{
    int value = Throws();

    [Throws("System.IO.IOException")]
    public C()
    {
    }

    [Throws("System.IO.IOException")]
    static int Throws() => throw new System.IO.IOException();
}
