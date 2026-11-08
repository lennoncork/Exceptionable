// Tests a source-defined generic Throws attribute whose argument is not an exception.
// Expected behavior: report EXC004 because string does not derive from Exception.
namespace Exceptionable
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute<T> : System.Attribute
    {
    }
}
class C
{
    [Exceptionable.Throws<string>]
    void M()
    {
    }
}
