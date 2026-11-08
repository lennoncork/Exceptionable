// Tests a closed constructed exception type as a generic Throws argument.
// Expected behavior: report no diagnostics.
using System;
using Exceptionable;
class Wrapper<T> : Exception
{
}
class C
{
    [Throws<Wrapper<int>>]
    void M()
    {
        throw new Wrapper<int>();
    }
}
