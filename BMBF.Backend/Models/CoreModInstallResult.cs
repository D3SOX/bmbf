using System.Collections.Generic;
using BMBF.Resources;

namespace BMBF.Backend.Models;

/// <summary>
/// Represents the result of an operation installing core mods.
/// </summary>
public class CoreModInstallResult
{
    /// <summary>
    /// Core mods that were newly downloaded/added following the core mod operation
    /// </summary>
    public List<CoreMod> Added { get; set; } = new();

    /// <summary>
    /// Loaded but uninstalled core mods that were reinstalled following the core mod install
    /// </summary>
    public List<CoreMod> Installed { get; set; } = new();

    /// <summary>
    /// How many core mods failed to be downloaded in order to load them.
    /// </summary>
    public List<CoreMod> FailedToFetch { get; set; } = new();

    /// <summary>
    /// How many uninstalled core mods failed to install
    /// </summary>
    public List<CoreMod> FailedToInstall { get; set; } = new();

    /// <summary>
    /// The type of core mod index used for the install operation
    /// </summary>
    public CoreModResultType ResultType { get; set; }
}
