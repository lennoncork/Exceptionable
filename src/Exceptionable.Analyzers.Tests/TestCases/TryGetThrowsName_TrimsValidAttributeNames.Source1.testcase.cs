// Verifies Try Get Throws Name Trims Valid Attribute Names.

using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
}
[Exceptionable.Throws("  System.IO.IOException  ")]
class C { }

