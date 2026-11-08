// Tests a source-defined generic Throws attribute without the attributes package.
// Expected behavior: report EXC003 for IOException.
namespace Exceptionable
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute<T> : System.Attribute
    {
    }
}
class C
{
    [Exceptionable.Throws<System.IO.IOException>]
    void M()
    {
    }

    void Caller()
    {
        M();
    }
}
