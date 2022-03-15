namespace BMBF.Backend;

public interface IPercentageProgress : IProgress
{
    /// <summary>
    /// The percentage completed.
    /// </summary>
    float Percentage { get; set; }
}
