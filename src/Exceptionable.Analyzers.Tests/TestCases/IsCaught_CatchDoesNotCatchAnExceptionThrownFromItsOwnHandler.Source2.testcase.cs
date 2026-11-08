// Verifies Is Caught Catch Does Not Catch An Exception Thrown From Its Own Handler.

using System;
class C
{
    void M()
    {
        try { }
        catch { throw new InvalidOperationException(); }
    }
}

