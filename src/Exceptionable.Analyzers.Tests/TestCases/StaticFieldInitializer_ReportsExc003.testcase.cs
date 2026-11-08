// Tests a static field initializer against the static constructor.
// Expected behavior: report EXC003.
using Exceptionable;
class C
{
    static int value = Throws();

    [DoesNotThrow]
    static C()
    {
    }

    [Throws("System.IO.IOException")]
    static int Throws() => throw new System.IO.IOException();
}
