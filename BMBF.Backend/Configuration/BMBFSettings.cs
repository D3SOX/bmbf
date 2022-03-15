#nullable disable

using System;

namespace BMBF.Backend.Configuration;

// ReSharper disable once InconsistentNaming
public class BMBFSettings
{
    public const string Position = "BMBFSettings";
    
    public string BindAddress { get; set; }
    
    public int BindPort { get; set; }

    public string PackageId { get; set; }

    public string SongsPath { get; set; }

    public string PlaylistsPath { get; set; }

    public string RootDataPath { get; set; }

    public string SongsCacheName { get; set; }

    public string ModsDirectoryName { get; set; }

    public bool DeleteDuplicateSongs { get; set; }

    public bool DeleteInvalidSongs { get; set; }

    public bool DeleteInvalidMods { get; set; }
    
    public bool UpdateModStatusesAutomatically { get; set; }
    
    public int ModFilesDebounceDelay { get; set; }

    public bool UpdateCachesAutomatically { get; set; }

    public int SongFolderDebounceDelay { get; set; }

    public int PlaylistFolderDebounceDelay { get; set; }

    public string PatchingFolderName { get; set; }

    public string ConfigsPath { get; set; }

    public string ModFilesPath { get; set; }

    public string LibFilesPath { get; set; }

    public Uri BeatSaverBaseUri { get; set; }
}
