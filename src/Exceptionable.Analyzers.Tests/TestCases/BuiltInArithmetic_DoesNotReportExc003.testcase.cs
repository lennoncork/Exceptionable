// Tests built-in arithmetic. It has no Exceptionable contract.
// Expected behavior: report no diagnostics.
class C
{
    void M(int left, int right)
    {
        _ = left + right;
    }
}
