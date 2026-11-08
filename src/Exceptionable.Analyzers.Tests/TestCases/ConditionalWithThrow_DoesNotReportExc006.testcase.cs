// Tests a conditional whose branch throws. The catch can still observe that exception.
// Expected behavior: do not report EXC006.
using Exceptionable;
class C
{
    [DoesNotThrow]
    void Safe()
    {
    }

    void M(bool flag)
    {
        try
        {
            if (flag)
            {
                Safe();
            }
            else
            {
                throw new System.Exception();
            }
        }
        catch
        {
        }
    }
}
