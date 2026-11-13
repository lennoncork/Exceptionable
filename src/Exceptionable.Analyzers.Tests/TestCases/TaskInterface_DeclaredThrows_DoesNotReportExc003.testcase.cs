// Tests an interface method that returns Task and declares [Throws].
// Expected behavior: do not report EXC003 at the call. The contract may be observed on await.
using System.Threading.Tasks;
using Exceptionable;
interface IWork
{
    [Throws("System.IO.IOException")]
    Task ReadAsync();
}
class C
{
    [DoesNotThrow]
    void Caller(IWork work)
    {
        _ = work.ReadAsync();
    }
}
