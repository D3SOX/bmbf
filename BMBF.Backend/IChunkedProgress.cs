namespace BMBF.Backend;

public interface IChunkedProgress : IProgress
{
    /// <summary>
    /// The current number of items completed.
    /// </summary>
    int ItemsCompleted { get; set; }
    
    /// <summary>
    /// The total number of items.
    /// </summary>
    int TotalItems { get; }
}
