// Tests a user-defined conversion.
// Expected behavior: report EXC003 for the explicit conversion.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    public static explicit operator int(C value)
    {
        _ = value;
        throw new System.IO.IOException();
    }

    void M(C value)
    {
        _ = (int)value;
    }
}
