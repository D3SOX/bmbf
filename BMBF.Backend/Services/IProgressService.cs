using System;

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
    /// Creates a percentage progress bar.
    /// </summary>
    /// <param name="name">The name of the progress bar.</param>
    /// <returns>The created progress bar.</returns>
    IPercentageProgress CreatePercentageProgress(string name);

    /// <summary>
    /// Creates a progress bar which functions on individual items.
    /// </summary>
    /// <param name="name">The name of the progress bar</param>
    /// <param name="maxItems">The number of items being processed</param>
    /// <returns>The created progress bar.</returns>
    IChunkedProgress CreateChunkedProgress(string name, int maxItems);
}
