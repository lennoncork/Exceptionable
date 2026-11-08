// Tests that an Exception constructor argument does not replace the thrown type.
// Expected behavior: do not report EXC002 for the declared exception.
using Exceptionable;
class ContractException : System.Exception
{
    public ContractException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}
class C
{
    [Throws("ContractException")]
    void M()
    {
        throw new ContractException("failed", new System.Exception());
    }
}
