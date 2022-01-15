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
}