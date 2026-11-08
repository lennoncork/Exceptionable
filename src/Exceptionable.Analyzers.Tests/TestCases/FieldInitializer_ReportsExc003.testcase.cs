// Tests a field initializer. It runs in the instance constructor.
// Expected behavior: report EXC003 because the constructor is [DoesNotThrow].
using Exceptionable;
[DoesNotThrow]
class C
{
    int value = Throws();

    [Throws("System.IO.IOException")]
    static int Throws() => throw new System.IO.IOException();
}
