using System.Collections.Generic;
using BMBF.Resources;

namespace BMBF.Backend.Models.Setup;

public class DowngradingStatus
{
    /// <summary>
    /// The diffs being used to downgrade the game, in order
    /// </summary>
    public List<DiffInfo> Path { get; set; } = new();

    /// <summary>
    /// The diff next to be downgraded using/currently being downgraded with
    /// </summary>
    public int CurrentDiff { get; set; }
}