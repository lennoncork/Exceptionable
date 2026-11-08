// Tests conditionals and nameof around [DoesNotThrow] calls.
// Expected behavior: report EXC006 four times and do not report EXC003 for nameof.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    public int Value => 0;

    [DoesNotThrow]
    int Safe() => 0;

    void M(bool flag, bool other, C value)
    {
        try
        {
            if (flag)
            {
                Safe();
            }
            else
            {
                Safe();
            }
        }
        catch
        {
        }

        try
        {
            _ = flag ? Safe() : Safe();
        }
        catch
        {
        }

        try
        {
            if (flag && other)
            {
                Safe();
            }
        }
        catch
        {
        }

        try
        {
            Safe();
            _ = nameof(Value);
            _ = value?.Safe();
        }
        catch
        {
        }
    }
}
