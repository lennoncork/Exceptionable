// Tests an annotated implementation of an unannotated interface member.
// Expected behavior: do not report EXC009. The warning requires an interface
// contract, not the reverse.
using Exceptionable;
interface IApi
{
    void Execute();
}
class Implementation : IApi
{
    [Throws("System.IO.IOException")]
    public void Execute()
    {
    }
}
