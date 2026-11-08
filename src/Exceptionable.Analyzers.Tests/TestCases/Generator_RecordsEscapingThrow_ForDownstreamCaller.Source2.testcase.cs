// Verifies Generator Records Escaping Throw For Downstream Caller.
class Caller
{
    void M()
    {
        _ = new Library.Resource("a");
    }
}


