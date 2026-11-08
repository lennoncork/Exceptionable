// Verifies Generator Records Escaping Throw For Downstream Caller.
namespace Library;
public class Resource
{
    public Resource(string name)
    {
        if (name is null)
        {
            throw new System.ArgumentNullException(nameof(name));
        }

        throw new System.InvalidOperationException();
    }
}


