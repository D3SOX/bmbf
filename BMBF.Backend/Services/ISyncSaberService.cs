using System.Threading.Tasks;
using BMBF.Backend.Models;

namespace BMBF.Backend.Services;

public interface ISyncSaberService
{
    /// <summary>
    /// Gets the current SyncSaber config
    /// </summary>
    /// <returns>The current SyncSaber config</returns>
    Task<SyncSaberConfig> GetConfig();

    /// <summary>
    /// Overwrites the current SyncSaber config
    /// </summary>
    /// <param name="cfg">Config to overwrite with</param>
    Task OverwriteConfig(SyncSaberConfig cfg);

    /// <summary>
    /// Syncs all enabled feeds
    /// </summary>
    Task Sync();
}
