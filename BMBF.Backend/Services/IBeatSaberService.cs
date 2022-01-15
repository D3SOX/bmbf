using System;
using System.Threading.Tasks;
using BMBF.Backend.Models;

namespace BMBF.Backend.Services;

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
    /// Prompts the user to install Beat Saber from the given APK
    /// </summary>
    /// <param name="apkPath">Path to the Beat Saber APK to install</param>
    void TriggerInstall(string apkPath);

    /// <summary>
    /// Prompts the user to uninstall Beat Saber
    /// </summary>
    void TriggerUninstall();

    /// <summary>
    /// Called whenever Beat Saber is uninstalled or installed
    /// </summary>
    event EventHandler<InstallationInfo?> AppChanged;
}