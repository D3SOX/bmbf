#nullable disable

namespace BMBF
{
    // ReSharper disable once InconsistentNaming
    public class BMBFSettings
    {
        public const string Position = "BMBFSettings";
        
        public string PackageId { get; set; }

        public string SongsPath { get; set; }

        public string PlaylistsPath { get; set; }

        public string RootDataPath { get; set; }

        public string SongsCacheName { get; set; }
        
        public bool DeleteDuplicateSongs { get; set; }
        
        public bool DeleteInvalidSongs { get; set; }
        
        public bool UpdateCacheAutomatically { get; set; }
        
        public string PatchingFolderName { get; set; }
        
        public ResourceUris Resources { get; set; }
    }
}