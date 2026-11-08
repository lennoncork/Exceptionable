// Tests a Throws contract on an interface property.
// Expected behavior: report EXC003 at the getter call.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    int Value { get; }
}
class C
{
    void M(IApi api)
    {
        _ = api.Value;
    }
}
