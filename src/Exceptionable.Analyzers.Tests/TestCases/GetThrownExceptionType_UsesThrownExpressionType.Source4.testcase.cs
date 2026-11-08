// Verifies Get Thrown Exception Type Uses Thrown Expression Type.

using System;
class CustomException : Exception { }
class C
{
    void M() { throw new CustomException(); }
}

