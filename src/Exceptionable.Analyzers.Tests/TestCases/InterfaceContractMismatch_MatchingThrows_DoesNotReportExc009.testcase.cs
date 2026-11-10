// Tests an implementation that repeats the interface [Throws] contract.
// Expected behavior: do not report EXC009. String and generic forms resolve to
// the same exception type.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    [Throws<System.IO.IOException>]
    public void Execute()
    {
    }
}
