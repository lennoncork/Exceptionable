// Tests one method that implements two interface members with different contracts.
// Expected behavior: report EXC009 for each implemented member.
using Exceptionable;
interface IRead
{
    [Throws("System.IO.IOException")]
    void Execute();
}
interface IWrite
{
    [DoesNotThrow]
    void Execute();
}
class Implementation : IRead, IWrite
{
    public void Execute()
    {
    }
}
