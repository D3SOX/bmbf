using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMBF.Backend.Models.Setup;
using BMBF.Resources;

namespace BMBF.Backend.Services;

public interface ISetupService
{
    /// <summary>
    /// The current status of setup
    /// Null if setup is not ongoing.
    /// </summary>
    SetupStatus? CurrentStatus { get; }

    /// <summary>
    /// Loads a saved setup status from disk
    /// </summary>
    Task LoadCurrentStatusAsync();

    /// <summary>
    /// Sent when the setup status changes, i.e. one section of setup is done
    /// </summary>
    event EventHandler<SetupStatus>? StatusChanged;

    /// <summary>
    /// Sent when setup quits.
    /// Argument is true if setup completed successfully, false if setup was quit by the user before completing.
    /// </summary>
    event EventHandler<bool> SetupQuit;

    /// <summary>
    /// Copies the APK to a temporary location to start setup
    /// <exception cref="InvalidStageException">Setup has already started</exception>
    /// <exception cref="InvalidOperationException">Beat Saber is not installed</exception>
    /// </summary>
    Task BeginSetupAsync();

    /// <summary>
    /// Stops setup, if ongoing
    /// </summary>
    Task QuitSetupAsync();

    /// <summary>
    /// Begins downgrading beat saber with the given downgrade path
    /// </summary>
    /// <param name="downgradePath">The set of diffs to use for downgrading</param>
    /// <exception cref="InvalidStageException">If the APK has already been downgraded or patched</exception>
    Task DowngradeAsync(List<DiffInfo> downgradePath);

    /// <summary>
    /// Resumes a downgrading operation
    /// </summary>
    /// <exception cref="InvalidOperationException">If a downgrading operation was not already in progress</exception>
    Task ResumeDowngradeAsync();

    /// <summary>
    /// Patches the current Beat Saber APK
    /// <exception cref="InvalidStageException">If the APK has already been patched</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">If no modloader is built in to the APK,
    /// and downloading the modloader fails</exception>
    /// </summary>
    Task PatchAsync();

    /// <summary>
    /// Shows a prompt to the user to uninstall the current Beat Saber
    /// <exception cref="InvalidStageException">If the APK has not yet been patched, or has already been uninstalled</exception>
    /// </summary>
    Task TriggerUninstallAsync();

    /// <summary>
    /// Shows a prompt to the user to install the modded APK.
    /// <exception cref="InvalidStageException">If the APK has not yet been uninstalled</exception>
    /// </summary>
    Task TriggerInstallAsync();

    /// <summary>
    /// Finishes setup by installing core mods
    /// <exception cref="InvalidStageException">If the modded APK has not yet been installed</exception>
    /// </summary>
    Task FinalizeSetup();
}
