// Tests throw null. C# throws NullReferenceException when the operand is null.
// Expected behavior: report EXC001 for NullReferenceException.
using Exceptionable;
class C
{
    [DoesNotThrow]
    void M()
    {
        throw null;
    }
}
