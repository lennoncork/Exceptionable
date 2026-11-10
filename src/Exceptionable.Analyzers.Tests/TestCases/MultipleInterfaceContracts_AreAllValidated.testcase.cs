// Tests that every interface contract mapped to one implementation is enforced.
// Expected behavior: accept FileNotFoundException under both contracts and
// report EXC002 for ArgumentException, which is outside the IOException contract.
// The implementations omit matching attributes, so EXC009 is also reported.
using Exceptionable;
interface IAnyException
{
    [Throws("System.Exception")]
    void Execute();
}
interface IIoException
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class ValidImplementation : IAnyException, IIoException
{
    public void Execute()
    {
        throw new System.IO.FileNotFoundException();
    }
}
class InvalidImplementation : IAnyException, IIoException
{
    public void Execute()
    {
        throw new System.ArgumentException();
    }
}
