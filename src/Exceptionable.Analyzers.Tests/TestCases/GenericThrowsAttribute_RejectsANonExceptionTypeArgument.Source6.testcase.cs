// Verifies Generic Throws Attribute Rejects ANon Exception Type Argument.

using Exceptionable;
class C
{
    [Throws<string>]
    public void M() { }
}

