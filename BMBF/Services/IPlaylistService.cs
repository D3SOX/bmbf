#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMBF.Models;

namespace BMBF.Services
{
    /// <summary>
    /// Manages the BMBF playlist cache (in-memory only, we do not save it to disk since loading playlists is not particularly expensive)
    /// </summary>
    public interface IPlaylistService
    {
        /// <summary>
        /// Gets or loads the playlists from the playlist folder
        /// </summary>
        /// <returns>A dictionary of the loaded playlists. Key is playlist path</returns>
        ValueTask<IReadOnlyDictionary<string, Playlist>> GetPlaylistsAsync();
        
        /// <summary>
        /// Updates the current playlist cache
        /// If it has not yet been loaded, this will just load the cache
        /// </summary>
        Task UpdatePlaylistCacheAsync();

        /// <summary>
        /// Deletes the given playlist
        /// </summary>
        /// <param name="playlist">The playlist to delete</param>
        /// <returns>Whether or not the playlist was contained within the cache</returns>
        Task<bool> DeletePlaylistAsync(Playlist playlist);

        /// <summary>
        /// Invoked whenever a playlist is added
        /// </summary>
        event EventHandler<Playlist> PlaylistAdded;
        
        /// <summary>
        /// Invoked whenever a playlist is deleted
        /// </summary>
        event EventHandler<Playlist> PlaylistDeleted;
    }
}