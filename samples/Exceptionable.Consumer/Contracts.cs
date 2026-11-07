using System;
using System.IO;
using Exceptionable;

internal static class Contracts
{
    [Throws("System.IO.IOException")]
    internal static void MayThrow()
    {
    }

    [DoesNotThrow]
    internal static void HandlesKnownException()
    {
        try
        {
            MayThrow();
        }
        catch (IOException)
        {
        }
    }
}
