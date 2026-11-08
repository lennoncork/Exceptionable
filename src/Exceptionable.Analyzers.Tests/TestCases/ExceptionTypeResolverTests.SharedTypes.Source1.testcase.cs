// Verifies Exception Type Resolver Tests.Shared Types.

namespace First
{
    public class SharedException : System.Exception { }
    public class CustomException : System.Exception { }
}
namespace Second
{
    public class SharedException : System.Exception { }
}
namespace Other
{
    public class NotAnException { }
    public class CustomException { }
}
namespace Nested
{
    public class Outer
    {
        public class NestedException : System.Exception { }
    }
}
public class Outer
{
    public class NestedException : System.Exception { }
}
namespace N.Outer
{
    public class NestedException { }
}
namespace N
{
    public class Outer
    {
        public class NestedException : System.Exception { }
    }
}
namespace Ambiguous.Outer
{
    public class NestedException : System.Exception { }
}
namespace Ambiguous
{
    public class Outer
    {
        public class NestedException : System.Exception { }
    }
}

