// Verifies Generator Records Member Specific Contracts From Emitted Assembly.
class Caller
{
    void M(Library.Resource resource)
    {
        resource.Caught();
        resource.Suppressed();
    }
}


