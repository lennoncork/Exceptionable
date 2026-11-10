// Tests an implementation throwing a subtype of its interface contract.
// Expected behavior: do not report EXC002.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    [Throws("System.IO.IOException")]
    public void Execute()
    {
        throw new System.IO.FileNotFoundException();
    }
}
