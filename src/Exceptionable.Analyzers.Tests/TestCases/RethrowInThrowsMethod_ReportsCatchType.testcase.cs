// Tests the static catch type used for a bare rethrow.
// Expected behavior: report EXC002 with InvalidOperationException.
using Exceptionable;
class C
{
    [Throws("System.ArgumentException")]
    void M()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.InvalidOperationException)
        {
            throw;
        }
    }
}
