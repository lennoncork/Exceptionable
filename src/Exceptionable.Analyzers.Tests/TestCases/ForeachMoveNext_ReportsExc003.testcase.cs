// Tests the foreach pattern. MoveNext is invoked by the loop.
// Expected behavior: report EXC003 at the foreach keyword.
using Exceptionable;
class Enumerator
{
    [Throws("System.IO.IOException")]
    public bool MoveNext() => throw new System.IO.IOException();

    public int Current => 0;
}
class Sequence
{
    [DoesNotThrow]
    public Enumerator GetEnumerator() => new Enumerator();
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
