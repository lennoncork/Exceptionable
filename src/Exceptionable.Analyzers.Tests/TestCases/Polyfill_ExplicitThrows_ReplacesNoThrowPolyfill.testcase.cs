// An explicit [Throws] on an override replaces the no-throw polyfill.
// Expected behavior: the declared InvalidOperationException is not EXC001.
using Exceptionable;

class Widget
{
    [Throws("System.InvalidOperationException")]
    public override string ToString() => throw new System.InvalidOperationException();
}
