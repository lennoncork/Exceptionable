// Tests a derived interface that redeclares a member without its own contract.
// Expected behavior: report EXC003 using the base interface member's contract.
using Exceptionable;
interface IBase
{
    [Throws("System.IO.IOException")]
    void Execute();
}
interface IDerived : IBase
{
    void Execute();
}
class Caller
{
    void Call(IDerived value)
    {
        value.Execute();
    }
}
