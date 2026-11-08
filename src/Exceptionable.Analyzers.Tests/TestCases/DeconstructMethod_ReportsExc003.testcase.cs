// Tests a Deconstruct method invoked by a deconstruction assignment.
// Expected behavior: report EXC003 for Deconstruct.
using Exceptionable;
class Pair
{
    [Throws("System.IO.IOException")]
    public void Deconstruct(out int first, out int second)
    {
        first = 0;
        second = 0;
    }
}
class C
{
    void M(Pair pair)
    {
        (var first, var second) = pair;
        _ = first;
        _ = second;
    }
}
