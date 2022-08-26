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

    SetupQuit,
    SetupStatusUpdate,

    InstallationUpdated,

    ModAdded,
    ModRemoved,
    ModStatusChanged,

    ProgressAdded,
    ProgressRemoved,
    ProgressUpdated
}
