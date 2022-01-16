using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMBF.ModManagement
{
    /// <summary>
    /// Used to abstract additional mod importing logic.
    /// </summary>
    public interface IModManager
    {
        /// <summary>
        /// Lock for mod installations/uninstallations
        /// </summary>
        SemaphoreSlim InstallLock { get; }

        /// <summary>
        /// Runs arbitrary import logic and then caches/imports the mod using the given provider.
        /// </summary>
        /// <param name="modProvider">Mod provider to import the mod with</param>
        /// <param name="stream">Stream to import the mod from</param>
        /// <param name="fileName">File name for the mod</param>
        /// <returns>The loaded and added mod</returns>
        Task<IMod> CacheAndImportMod(IModProvider modProvider, Stream stream, string fileName);
    }
}