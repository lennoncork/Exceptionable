// Verifies Generator Records Only Members Accessible Outside Private Scopes.
class Friend
{
    void M(Resource resource)
    {
        resource.Internal();
    }
}


