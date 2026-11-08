// Tests two declared exceptions that share a short name.
// Expected behavior: report a separate EXC003 for each namespace-qualified type.
using Exceptionable;
namespace A
{
    class SharedException : System.Exception { }
}
namespace B
{
    class SharedException : System.Exception { }
}
class C
{
    [Throws("A.SharedException")]
    [Throws("B.SharedException")]
    public void M() { }

    public void Call()
    {
        M();
    }
}
