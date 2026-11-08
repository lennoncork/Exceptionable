// Tests GetEnumerator that returns IEnumerator. The call is immediate in foreach.
// Expected behavior: report EXC003 even though the return type is a deferred type.
using System.Collections;
using Exceptionable;
class Sequence : IEnumerable
{
    [Throws("System.IO.IOException")]
    public IEnumerator GetEnumerator() => throw new System.IO.IOException();

    IEnumerator IEnumerable.GetEnumerator() => throw new System.IO.IOException();
}
class C
{
    void M(Sequence sequence)
    {
        foreach (var value in sequence)
        {
            _ = value;
        }
    }
}
