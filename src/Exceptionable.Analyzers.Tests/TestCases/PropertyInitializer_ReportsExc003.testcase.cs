// Tests a property initializer. It runs in the instance constructor.
// Expected behavior: report EXC003 for the annotated call.
using Exceptionable;
class C
{
    public int Value { get; } = Throws();

    [Throws("System.IO.IOException")]
    static int Throws() => throw new System.IO.IOException();
}
