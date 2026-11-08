// Tests an exception escaping a [DoesNotThrow] operator.
// Expected behavior: report EXC001.
using Exceptionable;
class C
{
    [DoesNotThrow]
    public static C operator +(C left, C right)
    {
        _ = left;
        _ = right;
        throw new System.InvalidOperationException();
    }
}
