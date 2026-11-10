// Tests a class-level [Throws] that supplies the same contract as the interface
// member. Expected behavior: do not report EXC009. The implementing class
// declares the matching attribution.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
[Throws("System.IO.IOException")]
class Implementation : IApi
{
    public void Execute()
    {
    }
}
