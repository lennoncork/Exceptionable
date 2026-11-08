// Tests that a derived thrown type is covered by a base exception contract.
// Expected behavior: do not report EXC002.
using Exceptionable;
class BaseException : System.Exception
{
}
class DerivedException : BaseException
{
}
class C
{
    [Throws("BaseException")]
    void M()
    {
        throw new DerivedException();
    }
}
