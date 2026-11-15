// A throw inside ToString is caught by the same method.
// Expected behavior: no EXC001, and the catch is not EXC006.
class Widget
{
    public override string ToString()
    {
        try
        {
            throw new System.InvalidOperationException();
        }
        catch (System.Exception)
        {
            return "";
        }
    }
}
