// Tests a property that repeats the interface [Throws] contract.
// Expected behavior: do not report EXC009.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    int Value { get; }
}
class Implementation : IApi
{
    [Throws("System.IO.IOException")]
    public int Value => 0;
}
