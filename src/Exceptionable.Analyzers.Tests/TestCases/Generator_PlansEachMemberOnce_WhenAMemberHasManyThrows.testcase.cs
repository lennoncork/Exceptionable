// Verifies that many throws in one method, and initializer throws that share a constructor, expand to each member once.
// Expected behavior: Execute and the instance constructor are each a single planning candidate, and the recorded contract lists each escaping exception once.
namespace Library;

public class Resource
{
    public int A { get; } = 0 switch { _ => throw new System.InvalidOperationException() };

    public int B { get; } = 0 switch { _ => throw new System.ArgumentException() };

    public Resource()
    {
        throw new System.TimeoutException();
    }

    public void Execute(int kind)
    {
        if (kind == 0)
        {
            throw new System.InvalidOperationException();
        }

        if (kind == 1)
        {
            throw new System.ArgumentException();
        }

        throw new System.TimeoutException();
    }
}
