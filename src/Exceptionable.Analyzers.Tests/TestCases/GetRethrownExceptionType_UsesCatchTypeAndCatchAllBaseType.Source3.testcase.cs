// Verifies Get Rethrown Exception Type Uses Catch Type And Catch All Base Type.

using System;
class C
{
    void Typed()
    {
        try { }
        catch (InvalidOperationException) { throw; }
    }

    void CatchAll()
    {
        try { }
        catch { throw; }
    }
}

