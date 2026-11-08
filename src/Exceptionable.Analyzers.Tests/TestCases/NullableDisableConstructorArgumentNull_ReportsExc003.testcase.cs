// Tests a constructor declared where nullable annotations are disabled.
// Expected behavior: report EXC003 and do not report EXC007.
#nullable enable
class Ignored
{
    public string Value = "";
}
#nullable disable
class Resource
{
    public Resource(string name)
    {
        if (name is null)
        {
            throw new System.ArgumentNullException(nameof(name));
        }
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
