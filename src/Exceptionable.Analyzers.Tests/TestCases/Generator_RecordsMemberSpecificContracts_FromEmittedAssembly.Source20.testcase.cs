// Verifies Generator Records Member Specific Contracts From Emitted Assembly.
class Caller
{
    void M(Library.Resource resource)
    {
        try
        {
            resource.Execute();
        }
        catch (System.TimeoutException)
        {
        }
    }
}


