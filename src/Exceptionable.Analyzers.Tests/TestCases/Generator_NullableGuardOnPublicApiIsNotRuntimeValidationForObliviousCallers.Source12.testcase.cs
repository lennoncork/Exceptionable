// Verifies Generator Nullable Guard On Public Api Is Not Runtime Validation For Oblivious Callers.
#nullable enable
namespace Library;
public class Resource
{
    public Resource(string name)
    {
        if (name is null)
        {
            throw new System.ArgumentNullException(nameof(name));
        }
    }
}


