// Tests nameof on an annotated property. nameof does not invoke the accessor.
// Expected behavior: report no diagnostics.
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    public int Value => 0;

    void M()
    {
        _ = nameof(Value);
    }
}
