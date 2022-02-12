using System;

namespace BMBF.Backend;

/// <summary>
/// Thrown when attempting to run a setup stage when at the incorrect setup stage.
/// </summary>
public class InvalidStageException : Exception
{
    public InvalidStageException(string? message) : base(message) {}
}
