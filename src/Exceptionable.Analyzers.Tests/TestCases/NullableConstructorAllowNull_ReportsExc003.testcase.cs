// Tests ArgumentNullException for an [AllowNull] parameter.
// Expected behavior: the caller may pass null, so keep the implicit contract and do not report EXC007.
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
class Resource
{
    public Resource([AllowNull] string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
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
