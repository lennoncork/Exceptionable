// Verifies Generator Records Member Specific Contracts From Emitted Assembly.
using Exceptionable;
class Caller
{
    [Throws("System.TimeoutException")]
    void M(Library.Resource resource)
    {
        resource.Execute();
    }
}


