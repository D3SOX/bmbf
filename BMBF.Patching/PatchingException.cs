using System;

namespace BMBF.Patching;

public class PatchingException : Exception
{
    public PatchingException(string? message) : base(message)
    { }
}