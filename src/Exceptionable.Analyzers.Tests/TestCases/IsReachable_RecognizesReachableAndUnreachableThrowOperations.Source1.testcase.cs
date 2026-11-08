// Verifies Is Reachable Recognizes Reachable And Unreachable Throw Operations.

using System;
class C
{
    void M()
    {
        throw new InvalidOperationException();
        throw new ArgumentException();
    }
}

