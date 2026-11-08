// Verifies Generator Avoids Conflicting Helper Attribute.
namespace Exceptionable.Generation
{
    internal sealed class InferredThrowsAttribute : System.Attribute
    {
        public InferredThrowsAttribute(string memberId, string exceptionType) { }
    }

    internal sealed class InferredThrowsAttribute1 : System.Attribute
    {
        public InferredThrowsAttribute1(string memberId, string exceptionType) { }
    }
}

public class Resource
{
    public void Throw()
    {
        throw new System.InvalidOperationException();
    }
}


