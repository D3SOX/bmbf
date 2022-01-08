using Newtonsoft.Json;

namespace BMBF.Models
{
    public class FileImportResult
    {
        /// <summary>
        /// Type of file that the file was imported as
        /// </summary>
        public FileImportResultType Type { get; set; }
        
        /// <summary>
        /// If the file was imported as a song, this stores the information about the new song
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Song? ImportedSong { get; set; }
        
        /// <summary>
        /// If the file was imported as a playlist, this stores the imported playlist ID
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ImportedPlaylistId { get; set; }
        
        /// <summary>
        /// If the file was imported as a mod config, this stores the ID of the mod that the config was assigned to
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ConfigModId { get; set; }
        
        /// <summary>
        /// Error message, if importing the file failed
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Error { get; set; }

        /// <summary>
        /// If the file was imported with a copy extension, this stores the info about the copy extension
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public FileCopyInfo? FileCopyInfo { get; set; }

        public static FileImportResult CreateError(string error)
        {
            return new FileImportResult
            {
                Type = FileImportResultType.Failed,
                Error = error
            };
        }
    }
}