namespace BMBF.Models.Setup;

public enum SetupStage
{
    /// <summary>
    /// Can be skipped, e.g. if the current version is already moddable
    /// Alternatively, core mod developers (or power users) might want to mod latest before core mods are available
    /// </summary>
    Downgrading,
    Patching,
    UninstallingOriginal,
    InstallingModded,
        
    /// <summary>
    /// Installing core mods
    /// </summary>
    Finalizing
}