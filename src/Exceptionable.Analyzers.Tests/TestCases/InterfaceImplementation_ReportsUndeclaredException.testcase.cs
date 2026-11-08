// Tests an implementation throwing outside its interface contract.
// Expected behavior: report EXC002 with InvalidOperationException.
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
        throw new System.InvalidOperationException();
    }
}
