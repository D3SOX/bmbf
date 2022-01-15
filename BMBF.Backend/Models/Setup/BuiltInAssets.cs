using System.Collections.Generic;
using BMBF.Resources;
using Newtonsoft.Json;

namespace BMBF.Models.Setup;

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
    /// List of built in core mods>. Null if versioned assets are not included
    /// </summary>
    public List<CoreMod>? CoreMods { get; set; }
        
    /// <summary>
    /// Version of built in modloader
    /// </summary>
    public string ModLoaderVersion { get; set; }
        
    [JsonConstructor]
    public BuiltInAssets(string beatSaberVersion, string modLoaderVersion, List<CoreMod>? coreMods)
    {
        BeatSaberVersion = beatSaberVersion;
        ModLoaderVersion = modLoaderVersion;
        CoreMods = coreMods;
    }
}