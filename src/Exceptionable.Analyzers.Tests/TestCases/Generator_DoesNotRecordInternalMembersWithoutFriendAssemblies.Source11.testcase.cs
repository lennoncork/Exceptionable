// Verifies Generator Does Not Record Internal Members Without Friend Assemblies.
public class Resource
{
    internal void Internal()
    {
        throw new System.InvalidOperationException();
    }
}


