// Tests a property-level Throws contract. It applies to both accessors.
// Expected behavior: report EXC003 for the getter and the setter.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    public int Value { get; set; }

    void M()
    {
        _ = Value;
        Value = 1;
    }
}
