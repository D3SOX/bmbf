using System.Text.Json.Serialization;

namespace BMBF.Backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CoreModResultType
{
    /// <summary>
    /// The downloaded core mod index was used for the core mod install.
    /// This guarantees that all the mod versions were correct
    /// </summary>
    UsedDownloaded,
    
    /// <summary>
    /// Downloading the core mod index failed, so a built-in core mod index was used
    /// (successfully) for the core mod install.
    /// Thus, the core mod versions used may not be the most up to date
    /// </summary>
    UsedBuiltIn,

    /// <summary>
    /// Downloading the index succeeded, but no core mods were available for the current Beat Saber version
    /// </summary>
    NoneAvailableForVersion,
    
    /// <summary>
    /// Downloading the index failed, and the built-in core mods did NOT match the Beat Saber version.
    /// Core mods MAY be available for this Beat Saber version, but they could not be checked for online.
    /// </summary>
    NoneBuiltInForVersion,
    
    /// <summary>
    /// The core mods could not be fetched, and no core mods were built-in.
    /// </summary>
    FailedToFetch,
    
    /// <summary>
    /// Core mods were not installed as Beat Saber was not installed, so the mods to install could not be determined
    /// </summary>
    BeatSaberNotInstalled
}
