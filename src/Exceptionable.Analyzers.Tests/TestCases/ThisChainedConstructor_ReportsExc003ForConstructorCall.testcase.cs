// Tests a [DoesNotThrow] constructor that chains to a throwing constructor.
// The chain does not rerun field initializers, but the this() call can still throw.
// Expected behavior: report EXC003 for the this() call.
using Exceptionable;
class C
{
    int value = Throws();

    [Throws("System.IO.IOException")]
    public C(int input)
    {
        _ = input;
    }

    [DoesNotThrow]
    public C()
        : this(0)
    {
    }

    [Throws("System.IO.IOException")]
    static int Throws() => throw new System.IO.IOException();
}
