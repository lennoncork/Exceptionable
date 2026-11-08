// Tests deconstruction into a property. The target is assigned through the setter.
// Expected behavior: report EXC003 for the setter and not the getter.
using Exceptionable;
class C
{
    public int Value
    {
        [DoesNotThrow]
        get => 0;

        [Throws("System.InvalidOperationException")]
        set
        {
        }
    }

    void M()
    {
        (Value, var local) = (1, 2);
        _ = local;
    }
}
