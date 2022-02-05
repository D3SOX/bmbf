using System.Threading.Tasks;
using BMBF.Backend.Models;

namespace BMBF.Backend.Services;

/// <summary>
/// Manages the installation and downloading of core mods
/// </summary>
public interface ICoreModService
{
    /// <summary>
    /// Installs any missing core mods.
    /// </summary>
    /// <param name="refresh">Whether or not to re-download the index if it has already been fetched</param>
    /// <returns>The results of attempting to install the core mods</returns>
    Task<CoreModInstallResult> InstallAsync(bool refresh);
}
