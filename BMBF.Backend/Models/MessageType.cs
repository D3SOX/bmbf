namespace BMBF.Backend.Models;

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
