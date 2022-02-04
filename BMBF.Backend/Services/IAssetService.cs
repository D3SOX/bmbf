using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Resources;
using SemanticVersioning;

namespace BMBF.Backend.Services;

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
    /// <returns>The available core mods for different Beat Saber versions. This will be null if no built-in core mods are available and internet is unavailable.
    /// <code>downloaded</code> will be set to false if the built-in core mods index is being used</returns>
    Task<(Dictionary<string, CoreMods> coreMods, bool downloaded)?> GetCoreMods();

    /// <summary>
    /// Extracts the given core mod to the given path.
    /// This may download the core mod if it is not builtin
    /// </summary>
    /// <param name="coreMod">Core mod to download</param>
    /// <returns>A stream which can be used to read the core mod</returns>
    Task<Stream> ExtractOrDownloadCoreMod(CoreMod coreMod);

    /// <summary>
    /// Downloads the given diff and returns a stream to read it
    /// </summary>
    /// <param name="diffInfo">The diff to download</param>
    /// <param name="ct">Token to cancel the diff download</param>
    /// <exception cref="HttpRequestException">Delta could not be downloaded</exception>
    /// <returns>Seekable stream to read the delta from</returns>
    Task<Stream> GetDelta(DiffInfo diffInfo, CancellationToken ct = default);

    /// <summary>
    /// Gets an index of diffs for downgrading
    /// </summary>
    /// <exception cref="HttpRequestException">If the diffs can not be fetched, for example due to lack of internet</exception>
    /// <returns>A list of the available diffs for downgrading</returns>
    Task<List<DiffInfo>> GetDiffs();

    /// <summary>
    /// Gets streams of libmain.so and libmodloader.so, and the version of the modloader that they represent.
    /// Will use the inbuilt modloader if no internet is available, or if the modloader in BMBF resources
    /// is the same version as the inbuilt modloader
    ///
    /// Otherwise, the modloader will be downloaded
    /// </summary>
    /// <param name="is64Bit">Whether or not to use the 64 bit modloader</param>
    /// <param name="ct">Token to cancel the modloader download</param>
    Task<(Stream modloader, Stream main, Version version)> GetModLoader(bool is64Bit, CancellationToken ct = default);

    /// <summary>
    /// Gets a stream to read the unstripped libunity.so for the given Beat Saber version
    /// </summary>
    /// <param name="ct">Token to cancel the libunity.so download</param>
    /// <param name="beatSaberVersion">Beat Saber version name to download the libunity.so for</param>
    /// <returns>Stream to read the libunity.so for the given Beat Saber version, or null if no libunity.so
    /// is available for the Beat Saber version</returns>
    /// <exception cref="HttpRequestException">If no libunity.so for this version is built-in, and download it failed</exception>
    Task<Stream?> GetLibUnity(string beatSaberVersion, CancellationToken ct = default);

    /// <summary>
    /// Gets or uses the inbuilt file copy extensions
    /// </summary>
    /// <exception cref="HttpRequestException">If no extensions were built in, and requesting them from BMBF resources failed</exception>
    /// <returns>The inbuilt or downloaded file copy extensions</returns>
    Task<FileExtensions> GetExtensions();
}
