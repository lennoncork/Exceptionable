// Verifies Get Effective Contract Disjoint Redeclared Contracts Allow Nothing.

using System;
namespace Exceptionable
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class ThrowsAttribute : Attribute
    {
        public ThrowsAttribute(string name) { }
    }
}
interface IFirst
{
    [Exceptionable.Throws("System.IO.IOException")]
    void Execute();
}
interface ISecond
{
    [Exceptionable.Throws("System.ArgumentException")]
    void Execute();
}
interface IDerived : IFirst, ISecond
{
    void Execute();
}

