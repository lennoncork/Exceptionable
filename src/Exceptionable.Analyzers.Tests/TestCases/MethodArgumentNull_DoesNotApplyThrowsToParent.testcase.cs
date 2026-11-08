// Tests an ordinary method that throws ArgumentNullException and the method that calls it.
// Expected behavior: do not apply [Throws] to either method.
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
}
