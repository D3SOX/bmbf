using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models;
using BMBF.Backend.Models.Setup;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using BMBF.Patching;
using BMBF.Resources;
using Octodiff.Core;
using Serilog;

namespace BMBF.Backend.Implementations;

/// <summary>
/// Some notes about this implementation.
/// - During downgrading, the APK is copied to a new location each downgrade since Octodiff requires outputting to a
/// different file, I can't do much about this. The old APK is deleted once diff patching is done.
/// - During patching, the APK is again copied to a new location. This is in case patching fails - we would have to
/// redowngrade if we did not have a spare copy of the APK
/// </summary>
public class SetupService : ISetupService, IDisposable
{
    public SetupStatus? CurrentStatus { get; private set; }

    public event EventHandler<SetupStatus>? StatusChanged;

    public event EventHandler<bool>? SetupQuit;

    private readonly IBeatSaberService _beatSaberService;
    private readonly IAssetService _assetService;

    private readonly string _setupDirName;
    private readonly string _statusFile;
    private readonly string _latestCompleteApkPath; // Stores the APK of the last completed stage
    private readonly string _tempApkPath; // Stores the APK of the currently executing stage
    private readonly string _backupPath; // Beat Saber data backup used during uninstall and reinstall
    private readonly ILogger _logger;
    private readonly string _backupOriginPath;

    private static readonly string[] DataFiles =
    {
        "PlayerData.dat",
        "settings.cfg",
        "LocalLeaderboards.dat",
        "LocalDailyLeaderboards.dat"
    };

    private CancellationTokenSource _cts = new();

    private bool _disposed;

    private readonly SemaphoreSlim _stageBeginLock = new(1);
    private readonly IFileSystem _io;
    private readonly Func<IPatchBuilder> _patcherFactory;
    private readonly ICoreModService _coreModService;

    public SetupService(IBeatSaberService beatSaberService,
        IAssetService assetService,
        BMBFSettings settings,
        IFileSystem io,
        Func<IPatchBuilder> patcherFactory,
        ICoreModService coreModService)
    {
        _beatSaberService = beatSaberService;
        _assetService = assetService;
        _patcherFactory = patcherFactory;
        _coreModService = coreModService;
        _setupDirName = Path.Combine(settings.RootDataPath, settings.PatchingFolderName);
        _statusFile = Path.Combine(_setupDirName, "status.json");
        _latestCompleteApkPath = Path.Combine(_setupDirName, "PostCurrentStage.apk");
        _tempApkPath = Path.Combine(_setupDirName, "CurrentStage.apk");
        _backupPath = Path.Combine(_setupDirName, "DataBackup");
        _backupOriginPath = settings.DataBackupBasePath;
        _logger = Log.Logger.ForContext(LogType.Setup);
        _io = io;

        _beatSaberService.AppChanged += OnBeatSaberServiceAppChanged;
    }

    public async Task LoadCurrentStatusAsync()
    {
        if (CurrentStatus != null) return;

        await _stageBeginLock.WaitAsync();
        try
        {
            await LoadSavedStatusAsync();
        }
        finally
        {
            _stageBeginLock.Release();
        }
    }

    private async Task LoadSavedStatusAsync()
    {
        if (CurrentStatus != null || !_io.File.Exists(_statusFile)) return;

        try
        {
            await using (var statusStream = _io.File.OpenRead(_statusFile))
            {
                CurrentStatus = await statusStream.ReadAsCamelCaseJsonAsync<SetupStatus>();

                // Update installing modded/uninstalling original status, since BS may have been installed or uninstalled since the status was saved
                await UpdateStatusPostPatching(await _beatSaberService.GetInstallationInfoAsync());
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load setup status");
            // Cancel any current setup operation
            QuitSetupInternal(false);
        }
    }

    private async Task ProcessStatusChange()
    {
        if (CurrentStatus == null) throw new NullReferenceException(nameof(CurrentStatus));
        StatusChanged?.Invoke(this, CurrentStatus);

        // Save new status for later
        if (_io.File.Exists(_statusFile)) _io.File.Delete(_statusFile);

        using var statusStream = _io.File.OpenWrite(_statusFile);
        await CurrentStatus.WriteAsCamelCaseJsonAsync(statusStream);
    }


    public async Task BeginSetupAsync()
    {
        await _stageBeginLock.WaitAsync();
        try
        {
            _cts = new CancellationTokenSource();

            await LoadSavedStatusAsync();
            if (CurrentStatus != null)
            {
                throw new InvalidStageException("Setup already started");
            }

            _logger.Information("Beginning setup");
            if (_io.Directory.Exists(_setupDirName)) _io.Directory.Delete(_setupDirName, true);
            _io.Directory.CreateDirectory(_setupDirName);

            _logger.Information("Copying APK to temp");
            var installInfo = await _beatSaberService.GetInstallationInfoAsync() ?? throw new InvalidOperationException("Cannot begin setup when Beat Saber is not installed");
            _io.File.Copy(installInfo.ApkPath, _latestCompleteApkPath);

            CurrentStatus = new SetupStatus(installInfo.Version);
            await ProcessStatusChange();
        }
        finally
        {
            _stageBeginLock.Release();
        }
    }

    public async Task QuitSetupAsync()
    {
        await _stageBeginLock.WaitAsync();
        try
        {
            if (CurrentStatus == null) return; // Setup not ongoing
            if (CurrentStatus.IsInProgress)
            {
                _cts.Cancel(); // Cancel the current setup stage
                Log.Warning("Attempted to quit setup while setup stage was in progress, waiting for the stage to pick up the cancellation");
                await Task.Delay(15000);
                if (CurrentStatus.IsInProgress)
                {
                    Log.Warning("Cancelling setup stage took too long - cannot quit setup");
                    return;
                }
            }

            QuitSetupInternal(false);
        }
        finally
        {
            _stageBeginLock.Release();
        }
    }

    private void QuitSetupInternal(bool isFinished)
    {
        Log.Information("Quitting setup");
        _io.Directory.Delete(_setupDirName, true);
        CurrentStatus = null;
        _cts.Dispose();
        SetupQuit?.Invoke(this, isFinished);
    }

    private async Task<SetupStatus> BeginSetupStage(SetupStage beginningStage, SetupStage? allowStage = null, bool updateNow = true)
    {
        await _stageBeginLock.WaitAsync();

        try
        {
            await LoadSavedStatusAsync();
            if (CurrentStatus == null)
            {
                throw new InvalidStageException("Setup not ongoing");
            }
            if (CurrentStatus.Stage != beginningStage && CurrentStatus.Stage != allowStage)
            {
                throw new InvalidStageException("Incorrect setup stage");
            }

            CurrentStatus.IsInProgress = true;
            CurrentStatus.Stage = beginningStage;
            if (updateNow)
            {
                await ProcessStatusChange();
            }
        }
        finally
        {
            _stageBeginLock.Release();
        }
        return CurrentStatus;
    }

    private async Task EndSetupStage()
    {
        if (CurrentStatus != null)
        {
            CurrentStatus.IsInProgress = false;

            // Failsafe delete temporary APK for this stage
            if (_io.File.Exists(_tempApkPath))
            {
                _io.File.Delete(_tempApkPath);
            }
            await ProcessStatusChange();
        }
    }

    public async Task DowngradeAsync(List<DiffInfo>? downgradePath)
    {
        var currentStatus = await BeginSetupStage(SetupStage.Downgrading, null, false);
        try
        {
            if (downgradePath != null)
            {
                currentStatus.DowngradingStatus = new DowngradingStatus { Path = downgradePath };
                await ProcessStatusChange();
            }

            if (currentStatus.DowngradingStatus == null)
            {
                throw new InvalidOperationException("Cannot resume downgrade - downgrade was not ongoing");
            }

            var deltaApplier = new DeltaApplier();
            for (int i = currentStatus.DowngradingStatus.CurrentDiff;
                 i < currentStatus.DowngradingStatus.Path.Count;
                 i++)
            {
                var diffInfo = currentStatus.DowngradingStatus.Path[i];

                _logger.Information($"Downloading patch from v{diffInfo.FromVersion} to v{diffInfo.ToVersion}");
                Stream? deltaStream = null;

                // Attempt to download the delta until canceled or until it succeeds
                while (deltaStream == null)
                {
                    try
                    {
                        deltaStream = await _assetService.GetDelta(diffInfo, _cts.Token);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException)
                        {
                            throw;
                        }

                        _logger.Error($"Failed to download diff: {ex.Message}");
                    }

                    _cts.Token.ThrowIfCancellationRequested();
                }

                await using var selectedDeltaStream = deltaStream;

                if (_io.File.Exists(_tempApkPath))
                {
                    _io.File.Delete(_tempApkPath);
                }

                _logger.Information($"Applying patch from v{diffInfo.FromVersion} to v{diffInfo.ToVersion}");
                await using (var basisStream = _io.File.OpenRead(_latestCompleteApkPath))
                await using (var tempStream = _io.File.Open(_tempApkPath, FileMode.Create, FileAccess.ReadWrite))
                {
                    await Task.Run(() => deltaApplier.Apply(basisStream,
                        new BinaryDeltaReader(selectedDeltaStream, new DummyProgressReporter()), tempStream));
                }

                // Move our APK back to the latest complete path, then move to the next diff
                _io.File.Delete(_latestCompleteApkPath);
                _io.File.Move(_tempApkPath, _latestCompleteApkPath);

                currentStatus.DowngradingStatus.CurrentDiff = i + 1;
                currentStatus.CurrentBeatSaberVersion = diffInfo.ToVersion;
                await ProcessStatusChange(); // Save the new status for resuming later on
            }

            Log.Information("Downgrading complete");
            currentStatus.Stage = SetupStage.Patching;
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Downgrading canceled");
        }
        finally
        {
            currentStatus.DowngradingStatus = null;
            // Move forward to the patching stage
            await EndSetupStage();
        }
    }

    public Task ResumeDowngradeAsync() => DowngradeAsync(null);

    public async Task PatchAsync()
    {
        // Allow downgrading to be skipped
        var currentStatus = await BeginSetupStage(SetupStage.Patching, SetupStage.Downgrading);
        try
        {
            _logger.Information("Beginning patching");

            if (_io.File.Exists(_tempApkPath))
            {
                _io.File.Delete(_tempApkPath);
            }
            _io.File.Copy(_latestCompleteApkPath, _tempApkPath);

            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (assemblyVersion == null)
            {
                throw new NullReferenceException("Assembly version could not be determined");
            }

            const string libFolder = "lib/arm64-v8a";

            // Download/extract necessary files for patching
            var modloader = await _assetService.GetModLoader(true, _cts.Token);
            await using var modloaderStream = modloader.modloader;
            await using var mainStream = modloader.main;
            var modloaderVersion = modloader.version;
            _logger.Debug($"Using modloader version: {modloaderVersion}");

            var builder = _patcherFactory()
                .WithModloader("QuestLoader", modloaderVersion)
                .ModifyFile($"{libFolder}/libmain.so", OverwriteMode.MustExist, mainStream)
                .ModifyFile($"{libFolder}/libmodloader.so", OverwriteMode.MustBeNew, modloaderStream)
                .ModifyManifest(manifest =>
                {
                    _logger.Information("Patching manifest");
                    manifest.AddPermission("android.permission.READ_EXTERNAL_STORAGE");
                    manifest.AddPermission("android.permission.WRITE_EXTERNAL_STORAGE");
                    manifest.AddPermission("android.permission.MANAGE_EXTERNAL_STORAGE");
                    manifest.AddPermission("oculus.permission.handtracking");
                    manifest.AddPermission("com.oculus.permission.HAND_TRACKING");
                    manifest.AddFeature("oculus.software.handtracking");
                    manifest.SetDebuggable(true);
                    manifest.SetRequestLegacyExternalStorage(true);
                })
                .ModifyFileAsync($"{libFolder}/libunity.so", OverwriteMode.MustExist, async ct =>
                {
                    _logger.Information("Downloading unstripped libunity.so . . .");
                    try
                    {
                        return await _assetService.GetLibUnity(currentStatus.CurrentBeatSaberVersion, ct);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.Warning("Could not download libunity.so - skipping!");
                        _logger.Verbose(ex, "libunity.so error");
                        return null;
                    }
                })
                .Sign(CertificateProvider.DebugCertificate);

            await builder.PatchAsync(_io, _tempApkPath, _logger, _cts.Token);

            // Move the current APK back to the latest complete
            _io.File.Delete(_latestCompleteApkPath);
            _io.File.Move(_tempApkPath, _latestCompleteApkPath);

            // Trigger the next stage
            await UpdateStatusPostPatching(await _beatSaberService.GetInstallationInfoAsync(), true);
            _logger.Information("Patching complete");
        }
        catch (OperationCanceledException)
        {
            Log.Information("Patching canceled");
        }
        finally
        {
            await EndSetupStage();
        }
    }

    /// <summary>
    /// Fixes the post-patching setup status.
    /// Useful if BS is installed/uninstalled - the status may have to be rolled back to uninstalling vanilla BS,
    /// or rolled forward to finalizing - depending on the new app
    /// </summary>
    /// <param name="installationInfo">The new Beat Saber install</param>
    /// <param name="force">Whether or not to update the status even if not in a post-patch stage</param>
    private async Task UpdateStatusPostPatching(InstallationInfo? installationInfo, bool force = false)
    {
        if (CurrentStatus == null) return;
        // If not in a post patching status, this can be safely skipped
        if (CurrentStatus.Stage != SetupStage.InstallingModded
            && CurrentStatus.Stage != SetupStage.UninstallingOriginal
            && CurrentStatus.Stage != SetupStage.Finalizing
            && !force)
        {
            return;
        }

        if (installationInfo == null)
        {
            // Beat Saber was uninstalled, so we are ready to install modded Beat Saber
            if (CurrentStatus.Stage != SetupStage.InstallingModded)
            {
                CurrentStatus.Stage = SetupStage.InstallingModded;
                await ProcessStatusChange();
                _logger.Information("Beat Saber was uninstalled");
            }
        }
        else if (installationInfo.ModTag == null)
        {
            // Non-modded Beat Saber was installed, so we need to uninstall it
            if (CurrentStatus.Stage != SetupStage.UninstallingOriginal)
            {
                CurrentStatus.Stage = SetupStage.UninstallingOriginal;
                await ProcessStatusChange();
                _logger.Information("Unmodded Beat Saber was installed. It will have to be uninstalled");
            }
        }
        else
        {
            if (CurrentStatus.Stage != SetupStage.Finalizing)
            {
                CurrentStatus.Stage = SetupStage.Finalizing;
                await ProcessStatusChange();
                _logger.Information("Modded Beat Saber was installed (woohoo)");
            }
        }
    }

    public async Task TriggerUninstallAsync()
    {
        await BeginSetupStage(SetupStage.UninstallingOriginal);
        try
        {
            if (!_io.Directory.Exists(_backupPath))
            {
                _io.Directory.CreateDirectory(_backupPath);
                foreach (string fileName in DataFiles)
                {
                    var filePath = Path.Combine(_backupOriginPath, fileName);
                    var backupFilePath = Path.Combine(_backupPath, fileName);

                    if (_io.File.Exists(filePath))
                    {
                        _logger.Information($"Backing up {fileName}");
                        _io.File.Copy(filePath, backupFilePath);
                    }
                    else
                    {
                        _logger.Information($"{fileName} did not exist");
                    }
                }
            }

            _beatSaberService.TriggerUninstall();
        }
        finally
        {
            await EndSetupStage();
        }
    }

    public async Task TriggerInstallAsync()
    {
        await BeginSetupStage(SetupStage.InstallingModded);
        try
        {
            _beatSaberService.TriggerInstall(_latestCompleteApkPath);
        }
        finally
        {
            await EndSetupStage();
        }
    }

    public async Task FinalizeSetup()
    {
        await BeginSetupStage(SetupStage.Finalizing);
        try
        {
            if (_io.Directory.Exists(_backupPath))
            {
                _io.Directory.CreateDirectory(_backupOriginPath);
                string[]? files = _io.Directory.GetFiles(_backupPath);
                _logger.Information($"Restoring {files.Length} files");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    _io.File.Copy(file, Path.Combine(_backupOriginPath, fileName));
                }
            }
            else
            {
                _logger.Warning("Could not find backup to restore");
            }

            // Install core mods
            await _coreModService.InstallAsync(true);

            // Install a song by default?

            QuitSetupInternal(true);
            Log.Information("Setup finished");
            // All done!
        }
        finally
        {
            await EndSetupStage();
        }
    }

    private async void OnBeatSaberServiceAppChanged(object? sender, InstallationInfo? installationInfo)
    {
        if (CurrentStatus == null) return;

        try
        {
            await UpdateStatusPostPatching(installationInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update status upon app change");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (CurrentStatus is { IsInProgress: true })
        {
            Log.Warning("Setup stage in progress while shutting down. This will be forcefully aborted!");

            // Make sure that the status isn't saved as in-progress
            CurrentStatus.IsInProgress = false;
            ProcessStatusChange().Wait();
        }
        _cts.Dispose();
        _stageBeginLock.Dispose();
    }
}
