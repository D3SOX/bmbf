using System;
using System.Collections.Generic;

namespace BMBF.Backend.Services;

/// <summary>
/// Manages progress bars
/// </summary>
public interface IProgressService
{
    /// <summary>
    /// Invoked when the progress of a particular operation is updated
    /// </summary>
    event EventHandler<IProgress> Updated;

    /// <summary>
    /// Invoked when an operation is added
    /// </summary>
    event EventHandler<IProgress> Added;

    /// <summary>
    /// Invoked when an operation is removed (this does not necessarily indicate its completion)
    /// </summary>
    event EventHandler<IProgress> Removed;

    /// <summary>
    /// A dictionary of the current operations with progress information.
    /// Key is <see cref="IProgress.Id"/>
    /// </summary>
    IReadOnlyDictionary<long, IProgress> CurrentOperations { get; }

    /// <summary>
    /// Creates a progress bar.
    /// </summary>
    /// <param name="name">The name of the progress bar.</param>
    /// <param name="total">The total number of items (AKA the "full" value of the bar)</param>
    /// <param name="representAsPercentage">Whether or not to represent the progress as a percentage</param>
    /// <param name="changeTolerance">If the progress is changed by an amount less than or equal to this value,
    /// the change will not be forwarded to the frontend</param>
    /// <param name="parent">Indicates the larger operation that this progress bar is part of</param>
    /// <returns>The created progress bar.</returns>
    IProgress CreateProgress(string name, int total, bool representAsPercentage = false, int changeTolerance = 0,
        IProgress? parent = null);

}
