// Tests a user-defined operator call.
// Expected behavior: report EXC003 for operator +.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    public static C operator +(C left, C right)
    {
        _ = left;
        _ = right;
        throw new System.IO.IOException();
    }

    void M(C left, C right)
    {
        _ = left + right;
    }
}
