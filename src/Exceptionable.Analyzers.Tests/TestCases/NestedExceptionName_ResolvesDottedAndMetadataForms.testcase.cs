// Tests nested exception names in dotted and metadata form.
// Expected behavior: both names resolve, so the throws are inside the contract.
using Exceptionable;
class Outer
{
    public class NestedException : System.Exception
    {
    }
}
namespace N
{
    class Outer
    {
        public class NestedException : System.Exception
        {
        }
    }
}
class C
{
    [Throws("Outer.NestedException")]
    void Dotted()
    {
        throw new Outer.NestedException();
    }

    [Throws("Outer+NestedException")]
    void Metadata()
    {
        throw new Outer.NestedException();
    }

    [Throws("N.Outer.NestedException")]
    void NamespacedDotted()
    {
        throw new N.Outer.NestedException();
    }

    [Throws("N.Outer+NestedException")]
    void NamespacedMetadata()
    {
        throw new N.Outer.NestedException();
    }
}
