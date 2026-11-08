// Tests a derived class contract replacing the base class contract.
// Expected behavior: report EXC001 and do not report EXC005.
using Exceptionable;
[Throws("System.IO.IOException")]
class Base
{
}
[DoesNotThrow]
class Derived : Base
{
    public void M()
    {
        throw new System.IO.IOException();
    }
}
