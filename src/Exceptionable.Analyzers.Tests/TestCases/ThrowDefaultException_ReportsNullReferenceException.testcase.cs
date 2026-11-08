// Tests throw default(Exception). A null exception operand becomes NullReferenceException.
// Expected behavior: report EXC001 for NullReferenceException.
using Exceptionable;
class C
{
    [DoesNotThrow]
    void M()
    {
        throw default(System.Exception);
    }
}
