// Verifies Is Does Not Throw Recognizes The Exceptionable Attribute.

using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class DoesNotThrowAttribute : Attribute { }
}
[Exceptionable.DoesNotThrow]
class C { }

