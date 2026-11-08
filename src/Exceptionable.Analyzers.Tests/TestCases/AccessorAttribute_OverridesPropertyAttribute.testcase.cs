// Tests an accessor contract replacing the property contract.
// Expected behavior: report EXC003 only for the setter.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    public int Value
    {
        [DoesNotThrow]
        get => 0;
        set
        {
        }
    }

    void M()
    {
        _ = Value;
        Value = 1;
    }
}
