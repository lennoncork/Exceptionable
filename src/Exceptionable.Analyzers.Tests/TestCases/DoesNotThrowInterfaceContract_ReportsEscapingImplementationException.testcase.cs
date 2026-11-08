// Tests that a source implementation honors an interface DoesNotThrow contract.
// Expected behavior: report EXC001 for the escaping implementation exception.
using Exceptionable;
interface IApi
{
    [DoesNotThrow]
    void Execute();
}
class Implementation : IApi
{
    public void Execute()
    {
        throw new System.InvalidOperationException();
    }
}
