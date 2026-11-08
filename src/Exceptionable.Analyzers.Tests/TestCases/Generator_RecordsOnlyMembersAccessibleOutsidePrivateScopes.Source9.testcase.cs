// Verifies Generator Records Only Members Accessible Outside Private Scopes.
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("ExceptionableTests")]

public class Resource
{
    public void Public() { throw new System.InvalidOperationException(); }
    protected void Protected() { throw new System.InvalidOperationException(); }
    internal void Internal() { throw new System.InvalidOperationException(); }
    private void Private() { throw new System.InvalidOperationException(); }

    private class PrivateNested
    {
        public void Hidden() { throw new System.InvalidOperationException(); }
    }

    public class PublicNested
    {
        public void Visible() { throw new System.InvalidOperationException(); }
    }
}


