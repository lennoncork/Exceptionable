// Verifies Is Reachable Keeps Constructor Body Separate From Field Initializer.

using System;
class C
{
    int value = 1;

    public C()
    {
        throw new InvalidOperationException();
        throw new ArgumentException();
    }
}

