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
    
    /// <summary>
    /// Total value that the progress represents (i.e. the full value)
    /// </summary>
    int Total { get; }
    
    /// <summary>
    /// The current progress, how many out of the total items have been completely
    /// </summary>
    int Completed { get; set; }
    
    /// <summary>
    /// If <see cref="Completed"/> changes by an amount less than or equal to this value, the change will not be
    /// forecast to the frontend.
    /// </summary>
    int ChangeTolerance { get; set; }
    
    /// <summary>
    /// Whether or not this progress will be represented as a percentage.
    /// </summary>
    bool RepresentAsPercentage { get; }
}
