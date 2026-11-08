// Tests an explicit [Throws] contract on a member that produces no known escaping exception.
// Expected behavior: keep the published contract and do not report EXC008.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    void M()
    {
    }
}
