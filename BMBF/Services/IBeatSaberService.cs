using System;
using System.Threading.Tasks;
using BMBF.Models;

namespace BMBF.Services;

/// <summary>
/// Manages the beat saber installation.
/// </summary>
public interface IBeatSaberService
{
    /// <summary>
    /// Gets the info about the current beat saber installation
    /// </summary>
    /// <returns>The info about the current beat saber installation, or null if beat saber is not installed</returns>
    Task<InstallationInfo?> GetInstallationInfoAsync();

    /// <summary>
    /// Called whenever Beat Saber is uninstalled or installed
    /// </summary>
    event EventHandler<InstallationInfo?> AppChanged;
}