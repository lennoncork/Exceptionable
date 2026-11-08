// Tests a user-declared record constructor with the copy-constructor signature.
// Expected behavior: report EXC003. This constructor runs field initializers.
using Exceptionable;
record R
{
    int value = Throws();

    [DoesNotThrow]
    public R(R other)
    {
        _ = other;
    }

    [Throws("System.IO.IOException")]
    static int Throws() => throw new System.IO.IOException();
}
