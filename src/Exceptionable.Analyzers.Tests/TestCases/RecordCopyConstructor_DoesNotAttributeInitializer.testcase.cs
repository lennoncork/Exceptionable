// Tests a record copy constructor. It copies fields and does not run initializers.
// Expected behavior: do not report EXC003 when the constructor that runs the initializer permits it.
using Exceptionable;
record R
{
    int value = Throws();

    [Throws("System.IO.IOException")]
    public R(int input)
    {
        _ = input;
    }

    [Throws("System.IO.IOException")]
    static int Throws() => throw new System.IO.IOException();
}
