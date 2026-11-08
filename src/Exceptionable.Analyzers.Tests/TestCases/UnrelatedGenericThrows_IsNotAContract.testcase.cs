// Tests a generic Throws attribute in another namespace.
// Expected behavior: report neither EXC003 nor EXC004.
namespace Other
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    sealed class ThrowsAttribute<T> : System.Attribute
    {
    }
}
class C
{
    [Other.Throws<System.IO.IOException>]
    void M()
    {
    }

    void Caller()
    {
        M();
    }
}
