// Tests an ordinary method that throws ArgumentNullException.
// Expected behavior: infer [Throws] on Child and report EXC003 at Parent.
// Grandparent calls Parent, which has no throw of its own, so it stays unspecified.
using System;
class Caller
{
    public void Child()
    {
        throw new ArgumentNullException("value");
    }

    public void Parent()
    {
        this.Child();
    }

    public void Grandparent()
    {
        this.Parent();
    }
}
