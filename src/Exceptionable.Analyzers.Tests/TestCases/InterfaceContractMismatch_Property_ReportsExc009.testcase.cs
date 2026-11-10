// Tests an auto-property that omits the interface property [Throws] contract.
// Expected behavior: report EXC009 on Value for the getter.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    int Value { get; }
}
class Implementation : IApi
{
    public int Value { get; }
}
