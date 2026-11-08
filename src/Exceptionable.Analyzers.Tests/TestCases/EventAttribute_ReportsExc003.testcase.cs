// Tests a Throws contract on an event. It applies to the add accessor.
// Expected behavior: report EXC003 for the add accessor.
using System;
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    public event Action Changed;

    void M()
    {
        Changed += () => { };
    }
}
