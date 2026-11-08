// Verifies Generator Resolves Closed Generic Exception Types Across Assemblies.
namespace Library;
public class GenericException<T> : System.Exception { }
public class Outer<T>
{
    public class NestedException<U> : System.Exception { }
}
public class Resource
{
    public void ThrowGeneric()
    {
        throw new GenericException<int>();
    }

    public void ThrowNested()
    {
        throw new Outer<string>.NestedException<int>();
    }

    public void ThrowArrayArgument()
    {
        throw new GenericException<int[]>();
    }
}


