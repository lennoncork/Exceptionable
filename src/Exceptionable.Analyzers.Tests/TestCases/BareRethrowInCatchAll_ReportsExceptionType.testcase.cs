// Tests the conservative static type used for a bare rethrow from catch-all.
// Expected behavior: report EXC002 with System.Exception.
using Exceptionable;
class C
{
    [Throws("System.InvalidOperationException")]
    void M()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch
        {
            throw;
        }
    }
}
