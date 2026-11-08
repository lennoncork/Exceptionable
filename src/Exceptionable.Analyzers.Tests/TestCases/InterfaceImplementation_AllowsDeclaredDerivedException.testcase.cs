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
    public void Execute()
    {
        throw new System.IO.FileNotFoundException();
    }
}
