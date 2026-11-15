// An explicit [Throws] on an override does not replace the no-throw polyfill.
// Expected behavior: EXC001 for the throw, and EXC009 because the override does not declare [DoesNotThrow].
using Exceptionable;

class Widget
{
    [Throws("System.InvalidOperationException")]
    public override string ToString() => throw new System.InvalidOperationException();
}
