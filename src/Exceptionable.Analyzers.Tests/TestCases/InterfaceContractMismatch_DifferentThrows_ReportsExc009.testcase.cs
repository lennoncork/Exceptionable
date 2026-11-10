// Tests an implementation that declares a different [Throws] set than the
// interface. Expected behavior: report EXC009. A derived exception type is not
// the same published contract.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    [Throws("System.IO.FileNotFoundException")]
    public void Execute()
    {
    }
}
