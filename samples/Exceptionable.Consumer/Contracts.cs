using System;
using System.IO;
using Exceptionable;

internal static class Contracts
{
    [Throws("System.IO.IOException")]
    internal static void MayThrow()
    {
        throw new IOException();
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

    [Throws("System.IO.IOException")]
    internal static void MismatchedContract()
    {
        throw new InvalidOperationException();
    }

    internal static void UnhandledInterfaceCall(IConsumerApi api)
    {
        api.Execute();
    }

    [DoesNotThrow]
    internal static void HandlesInterfaceCall(IConsumerApi api)
    {
        try
        {
            api.Execute();
        }
        catch (IOException)
        {
        }
    }
}

internal interface IConsumerApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
