// Tests an exception contract on a parameter that is not a delegate.
// Expected behavior: report EXC011.
using Exceptionable;
class C
{
    void M([DoesNotThrow] string value) { }
}
