// Tests subscribing and unsubscribing through separately annotated event accessors.
// Expected behavior: report two EXC003 diagnostics, one for each accessor call.
using Exceptionable;
using System;
class C {
    public event Action Changed {
        [Throws("System.IO.IOException")] add { }
        [Throws("System.InvalidOperationException")] remove { }
    }
    void Handler() { }
    void Caller() { Changed += Handler; Changed -= Handler; }
}
