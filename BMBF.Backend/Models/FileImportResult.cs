using System.Text.Json.Serialization;
using BMBF.ModManagement;

namespace BMBF.Backend.Models;

public class FileImportResult
{
    /// <summary>
    /// Type of file that the file was imported as
    /// </summary>
    public FileImportResultType Type { get; set; }

    /// <summary>
    /// If the file was imported as a song, this stores the information about the new song
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Song? ImportedSong { get; set; }

    /// <summary>
    /// If the file was imported as a playlist, this stores the imported playlist ID
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImportedPlaylistId { get; set; }

    /// <summary>
    /// If the file was imported as a mod config, this stores the ID of the mod that the config was assigned to
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConfigModId { get; set; }

    /// <summary>
    /// Error message, if importing the file failed
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    /// <summary>
    /// If the file was imported with a copy extension, this stores the info about the copy extension
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileCopyInfo? FileCopyInfo { get; set; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IMod? ImportedMod { get; set; }

    public static FileImportResult CreateError(string error)
    {
        return new FileImportResult
        {
            Type = FileImportResultType.Failed,
            Error = error
        };
    }
}