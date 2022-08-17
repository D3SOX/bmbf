using System.Text.Json.Serialization;

namespace BMBF.Backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
    SongAdded,
    SongRemoved,

    PlaylistUpdated,
    PlaylistAdded,
    PlaylistRemoved,

    SetupFinished,
    SetupStatusUpdate,

    InstallationUpdated,
    
    ModAdded,
    ModRemoved,
    ModStatusChanged,
    
    ProgressAdded,
    ProgressRemoved,
    ProgressUpdated
}
