using System.IO;
using System.Threading.Tasks;

namespace BMBF.Services
{
    /// <summary>
    /// Represents the BeatSaver song repository.
    /// </summary>
    public interface IBeatSaverService
    {
        /// <summary>
        /// Downloads a song, given its song hash.
        /// Uses the latest revision of the song.
        /// </summary>
        /// <param name="hash">Hash of the song to upload</param>
        /// <returns>Stream of the song download, or null if no song with the given hash existed on BeatSaver</returns>
        Task<Stream?> DownloadSongByHash(string hash);

        /// <summary>
        /// Downloads a song, given its beatsaver key
        /// Uses the latest revision of the song
        /// </summary>
        /// <param name="key">Beatsaver key of the song to download</param>
        /// <returns>Stream of the song download, or null if no song with the given key existed on BeatSaver</returns>
        Task<Stream?> DownloadSongByKey(string key);
    }
}