using System;
using BMBF.Backend.Services;

namespace BMBF.Backend;

/// <summary>
/// Represents the progress of an operation.
/// Disposing this object will remove it from the <see cref="IProgressService"/>
/// </summary>
public interface IProgress : IDisposable
{
    /// <summary>
    /// The name of the operation that this object represents the progress of.
    /// </summary>
    string Name { get; }
}
