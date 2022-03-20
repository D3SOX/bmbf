using System.IO;
using System.Threading.Tasks;
using BMBF.Backend.Models;

namespace BMBF.Backend.Services;

/// <summary>
/// Manages importing new playlists/mods/songs into BMBF.
/// </summary>
public interface IFileImporter
{
    /// <summary>
    /// Imports a file from the given stream.
    /// </summary>
    /// <param name="stream">Stream to import the file from</param>
    /// <param name="fileName">Name of the file</param>
    /// <returns>The result of the import operation, possibly containing an error - shows what file type the file was imported as</returns>
    Task<FileImportResult> TryImportAsync(Stream stream, string fileName);

    /// <summary>
    /// Imports a file from the given stream, throwing if importing fails.
    /// </summary>
    /// <param name="stream">Stream to import the file from</param>
    /// <param name="fileName">Name of the file</param>
    /// <exception cref="ImportException">If importing fails</exception>
    /// <returns>The result of the import operation - shows what file type the file was imported as</returns>
    Task<FileImportResult> ImportAsync(Stream stream, string fileName);
    
    /// <summary>
    /// Attempts to download the songs in the given playlist
    /// </summary>
    /// <param name="playlist">Playlist to download the songs from. Does not have to be part of the
    /// loaded playlist set</param>
    /// <param name="progressName">If non-null, specifies the name of a progress bar to use for updates</param>
    /// <param name="progressParent">If non-null, specifies the parent operations</param>
    Task DownloadSongs(Playlist playlist, string? progressName = null, IProgress? progressParent = null);
}
