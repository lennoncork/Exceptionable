// Verifies Is Caught Catch In Enclosing Try Handles The Exception.

using System;
class C
{
    void M()
    {
        try { throw new InvalidOperationException(); }
        catch (InvalidOperationException) { }
    }
}

