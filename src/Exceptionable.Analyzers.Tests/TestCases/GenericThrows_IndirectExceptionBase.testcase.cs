// Tests a generic Throws argument that derives from Exception through another type.
// Expected behavior: report no diagnostics.
using System;
using Exceptionable;
class Mid : Exception
{
}
class Leaf : Mid
{
}
class C
{
    [Throws<Leaf>]
    void M()
    {
        throw new Leaf();
    }
}
