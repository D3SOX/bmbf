using System.Collections.Generic;
using System.Text.Json.Serialization;
using BMBF.Resources;

namespace BMBF.Backend.Models.Setup;

/// <summary>
/// Stores information about the patching assets built into BMBF
/// </summary>
public class BuiltInAssets
{
    /// <summary>
    /// Beat Saber version that builtin core mods and libunity.so are for
    /// Null if versioned assets are not included
    /// </summary>
    public string? BeatSaberVersion { get; set; }
        
    /// <summary>
    /// List of built in core mods. Null if core mods are not included.
    /// </summary>
    public List<CoreMod>? CoreMods { get; set; }
        
    /// <summary>
    /// Version of built in modloader.
    /// Null if modloader is not built in.
    /// </summary>
    public string? ModLoaderVersion { get; set; }
}