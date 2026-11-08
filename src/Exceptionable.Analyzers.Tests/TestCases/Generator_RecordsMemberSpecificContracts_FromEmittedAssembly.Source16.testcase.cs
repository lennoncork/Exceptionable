// Verifies Generator Records Member Specific Contracts From Emitted Assembly.
#nullable enable
using Exceptionable;
namespace Library;
public class Resource
{
    public int field = 0 switch { _ => throw new System.IO.IOException() };

    public int Initialized { get; } = 0 switch { _ => throw new System.ObjectDisposedException("Initialized") };

    public int Value
    {
        get => throw new System.InvalidOperationException();
    }

    public void Execute()
    {
        throw new System.TimeoutException();
    }

    public void Caught()
    {
        try
        {
            throw new System.ArgumentException();
        }
        catch (System.ArgumentException)
        {
        }
    }

    [DoesNotThrow]
    public void Suppressed()
    {
        throw new System.NotSupportedException();
    }

    public Resource(string name)
    {
        if (name is null)
        {
            throw new System.ArgumentNullException(nameof(name));
        }
    }
}


