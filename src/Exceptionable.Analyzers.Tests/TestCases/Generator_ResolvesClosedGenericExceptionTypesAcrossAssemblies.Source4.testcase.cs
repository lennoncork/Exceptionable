// Verifies Generator Resolves Closed Generic Exception Types Across Assemblies.
class Caller
{
    void M(Library.Resource resource)
    {
        resource.ThrowGeneric();
    }
}


