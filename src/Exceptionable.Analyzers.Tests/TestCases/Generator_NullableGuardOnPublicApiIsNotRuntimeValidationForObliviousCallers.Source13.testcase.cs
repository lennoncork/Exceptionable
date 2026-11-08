// Verifies Generator Nullable Guard On Public Api Is Not Runtime Validation For Oblivious Callers.
#nullable disable
class Caller
{
    void M()
    {
        _ = new Library.Resource(null);
    }
}


