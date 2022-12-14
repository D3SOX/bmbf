using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Util;

namespace BMBF.Backend.Services;

/// <summary>
/// Manages the BMBF song cache
/// </summary>
public interface ISongService
{
    /// <summary>
    /// Gets a dictionary of all loaded songs.
    /// Keys are the song's SHA-1 hash.
    /// </summary>
    /// <returns>An enumerator over the loaded songs</returns>
    ValueTask<IReadOnlyDictionary<string, Song>> GetSongsAsync();

    /// <summary>
    /// Updates the current song cache
    /// </summary>
    Task UpdateSongCacheAsync();

    /// <summary>
    /// Attempts to import a song from the given provider.
    /// This involves attempting to parse the song, then saving it to the songs directory.
    /// </summary>
    /// <returns>The result of the import operation, with an error message if the given archive was not a valid song, or if the song already existed</returns>
    /// <param name="songProvider">Archive/directory to import the song from</param>
    /// <param name="fileName">Name of the archive the song is being imported from</param>
    Task<FileImportResult> ImportSongAsync(ISongProvider songProvider, string fileName);

    /// <summary>
    /// Deletes the song(s) with the given hash, if a song with the hash is loaded.
    /// </summary>
    /// <param name="hash">The SHA-1 hash of the song to delete, upper case</param>
    /// <returns>Whether or not any songs with the given hash existed and were deleted</returns>
    Task<bool> DeleteSongAsync(string hash);

    /// <summary>
    /// Invoked whenever a new song is loaded.
    /// NOT invoked upon initial song load, this only handles updates to the song cache after it is loaded.
    /// </summary>
    event EventHandler<Song>? SongAdded;

    /// <summary>
    /// Invoked whenever a song is removed
    /// </summary>
    event EventHandler<Song>? SongRemoved;
}
