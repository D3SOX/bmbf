using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BMBF.Resources;
using System.Net.Http;

namespace BMBF.Services
{
    /// <summary>
    /// Manages resource files that BMBF needs
    /// </summary>
    public interface IAssetService
    {
        /// <summary>
        /// Gets the Beat Saber version that the built-in libunity.so and core mods are for.
        /// Null if versioned-specific assets are not included with BMBF
        /// </summary>
        string? BuiltInAssetsVersion { get; }

        /// <summary>
        /// Gets the core mods that are currently available.
        /// </summary>
        /// <param name="refresh">Whether or not to redownload the core mods index if it has been fetched already</param>
        /// <returns>The available core mods for different Beat Saber versions. This will be empty if no built-in core mods are available and internet is unavailable</returns>
        Task<Dictionary<string, CoreMods>> GetCoreMods(bool refresh = false);

        /// <summary>
        /// Extracts the given core mod to the given path.
        /// This may download the core mod if it is not builtin
        /// </summary>
        /// <param name="coreMod">Core mod to download</param>
        /// <param name="path">Path to extract or download the core mod to</param>
        Task ExtractOrDownloadCoreMod(CoreMod coreMod, string path);

        /// <summary>
        /// Downloads the given diff and returns a stream to read it
        /// </summary>
        /// <param name="diffInfo">The diff to download</param>
        Task<Stream> GetDelta(DiffInfo diffInfo);

        /// <summary>
        /// Gets an index of diffs for downgrading
        /// </summary>
        /// <param name="refresh">Whether or not to redownload the diff index if it has already been fetched</param>
        /// <exception cref="HttpRequestException">If the diffs can not be fetched, for example due to lack of internet</exception>
        /// <returns></returns>
        Task<List<DiffInfo>> GetDiffs(bool refresh = false);
        
        /// <summary>
        /// Gets streams of libmain.so and libmodloader.so
        /// Will use the inbuilt modloader if no internet is available, or if the modloader in BMBF resources
        /// is the same version as the inbuilt modloader
        ///
        /// Otherwise, the modloader will be downloaded
        /// </summary>
        /// <param name="is64Bit">Whether or not to use the 64 bit modloader</param>
        Task<(Stream modloader, Stream main)> GetModLoader(bool is64Bit);

        /// <summary>
        /// Gets a stream to read the unstripped libunity.so for the given Beat Saber version
        /// </summary>
        Task<Stream?> GetLibUnity(string beatSaberVersion);
    }
}