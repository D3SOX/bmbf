using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Extensions;
using BMBF.Models;
using BMBF.Models.Setup;
using BMBF.Patching;
using BMBF.Resources;
using BMBF.Services;
using BMBF.Util;
using Octodiff.Core;
using Serilog;

namespace BMBF.Implementations;

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

    public event EventHandler? SetupComplete;

    private readonly IBeatSaberService _beatSaberService;
    private readonly IAssetService _assetService;
    private readonly TagManager _tagManager;

    private readonly string _setupDirName;
    private readonly string _statusFile;
    private readonly string _latestCompleteApkPath; // Stores the APK of the last completed stage
    private readonly string _tempApkPath; // Stores the APK of the currently executing stage
    private readonly string _backupPath; // Beat Saber data backup used during uninstall and reinstall
    private readonly string _packageId;
    private readonly ILogger _logger;

    private string BeatSaberDataPath => $"/sdcard/Android/data/{_packageId}/files";

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

    public SetupService(IBeatSaberService beatSaberService, IAssetService assetService, TagManager tagManager, BMBFSettings settings)
    {
        _beatSaberService = beatSaberService;
        _assetService = assetService;
        _tagManager = tagManager;
        _setupDirName = Path.Combine(settings.RootDataPath, settings.PatchingFolderName);
        _statusFile = Path.Combine(_setupDirName, "status.json");
        _latestCompleteApkPath = Path.Combine(_setupDirName, "PostCurrentStage.apk");
        _tempApkPath = Path.Combine(_setupDirName, "CurrentStage.apk");
        _backupPath = Path.Combine(_setupDirName, "DataBackup");
        _packageId = settings.PackageId;
        _logger = new LoggerConfiguration()
            .WriteTo.Logger(Log.Logger.ForContext<SetupService>())
            // TODO: Write to status update ws api
            .CreateLogger();


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
        if (CurrentStatus != null || !File.Exists(_statusFile)) return;
            
        await using var statusStream = File.OpenRead(_statusFile);
        CurrentStatus = statusStream.ReadAsCamelCaseJson<SetupStatus>();

        // Update installing modded/uninstalling original status, since BS may have been installed or uninstalled since the status was saved
        UpdateStatusPostPatching(await _beatSaberService.GetInstallationInfoAsync());
    }

    private void ProcessStatusChange()
    {
        if (CurrentStatus == null) throw new NullReferenceException(nameof(CurrentStatus));
        StatusChanged?.Invoke(this, CurrentStatus);
            
        // Save new status for later
        using var statusStream = File.OpenWrite(_statusFile);
        CurrentStatus.WriteAsCamelCaseJson(statusStream);
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
                throw new InvalidOperationException("Setup already started");
            }

            _logger.Information("Beginning setup");
            if(Directory.Exists(_setupDirName)) Directory.Delete(_setupDirName, true);
            Directory.CreateDirectory(_setupDirName);
            
            _logger.Information("Copying APK to temp");
            var installInfo = await _beatSaberService.GetInstallationInfoAsync() ?? throw new InvalidOperationException("Cannot begin setup when Beat Saber is not installed");
            File.Copy(installInfo.ApkPath, _latestCompleteApkPath);

            CurrentStatus = new SetupStatus(installInfo.Version);
            ProcessStatusChange();
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
                await Task.Delay(5000);
                if (CurrentStatus.IsInProgress)
                {
                    Log.Warning("Setup stage did not shut down, quitting setup anyway");
                }
            }
                
            QuitSetupInternal();
        }
        finally
        {
            _stageBeginLock.Release();
        }
    }

    private void QuitSetupInternal()
    {
        Log.Information("Quitting setup");
        Directory.Delete(_setupDirName, true);
        CurrentStatus = null;
        _cts.Dispose();
    }

    private async Task<SetupStatus> BeginSetupStage(SetupStage beginningStage, SetupStage? allowStage = null, bool updateNow = true)
    {
        await _stageBeginLock.WaitAsync();

        try
        {
            await LoadSavedStatusAsync();
            if (CurrentStatus == null) throw new InvalidOperationException("Setup not ongoing"); 
            if (CurrentStatus.Stage != beginningStage && CurrentStatus.Stage != allowStage) throw new InvalidOperationException("Incorrect setup stage");
                
            CurrentStatus.IsInProgress = true;
            CurrentStatus.Stage = beginningStage;
            if (updateNow)
            {
                ProcessStatusChange();
            }
        }
        finally
        {
            _stageBeginLock.Release();
        }
        return CurrentStatus;
    }
        
    private void EndSetupStage()
    {
        if (CurrentStatus != null)
        {
            CurrentStatus.IsInProgress = false;
            ProcessStatusChange();
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
                ProcessStatusChange();
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
                DiffInfo diffInfo = currentStatus.DowngradingStatus.Path[i];

                _logger.Information($"Downloading patch from v{diffInfo.FromVersion} to v{diffInfo.ToVersion}");
                Stream? deltaStream = null;

                // Attempt to download the delta multiple times
                while (deltaStream == null)
                {
                    try
                    {
                        deltaStream = await _assetService.GetDelta(diffInfo, _cts.Token);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) throw;
                        _logger.Error($"Failed to download diff: {ex.Message}");
                    }

                    _cts.Token.ThrowIfCancellationRequested();
                }

                if (File.Exists(_tempApkPath)) File.Delete(_tempApkPath);

                _logger.Information($"Applying patch from v{diffInfo.FromVersion} to v{diffInfo.ToVersion}");
                await using (var basisStream = File.OpenRead(_latestCompleteApkPath))
                await using (var tempStream = File.Open(_tempApkPath, FileMode.Create, FileAccess.ReadWrite))
                {
                    await Task.Run(() => deltaApplier.Apply(basisStream,
                        new BinaryDeltaReader(deltaStream, new DummyProgressReporter()), tempStream));
                }

                // Move our APK back to the latest complete path, then move to the next diff
                File.Delete(_latestCompleteApkPath);
                File.Move(_tempApkPath, _latestCompleteApkPath);

                currentStatus.DowngradingStatus.CurrentDiff = i + 1;
                currentStatus.CurrentBeatSaberVersion = diffInfo.ToVersion;
                ProcessStatusChange(); // Save the new status for resuming later on
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
            EndSetupStage();
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

            if (File.Exists(_tempApkPath)) File.Delete(_tempApkPath);
            File.Copy(_latestCompleteApkPath, _tempApkPath);

            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (assemblyVersion == null)
            {
                throw new NullReferenceException("Assembly version could not be determined");
            }
            var semVersion =
                new SemanticVersioning.Version(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
            var libFolder = "lib/arm64-v8a";

            // Download/extract necessary files for patching
            _logger.Information("Preparing modloader");
            var modloader = await _assetService.GetModLoader(true, _cts.Token);
            await using var modloaderStream = modloader.modloader;
            await using var mainStream = modloader.main;
            var modloaderVersion = modloader.version;

            _logger.Information("Preparing unstripped libunity.so");
            await using var unityStream = await _assetService.GetLibUnity(currentStatus.CurrentBeatSaberVersion, _cts.Token);

            var builder = new PatchBuilder("BMBF", semVersion, _tagManager)
                .WithModloader("QuestLoader", modloaderVersion)
                .ModifyFile($"{libFolder}/libmain.so", OverwriteMode.MustExist, () => mainStream)
                .ModifyFile($"{libFolder}/libmodloader.so", OverwriteMode.MustBeNew, () => modloaderStream)
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
                .Sign(CertificateProvider.DebugCertificate);

            if (unityStream is null)
            {
                _logger.Warning(
                    $"No libunity.so found for Beat Saber version {currentStatus.CurrentBeatSaberVersion}");
            }
            else
            {
                builder.ModifyFile($"{libFolder}/libunity.so", OverwriteMode.MustExist, () => unityStream);
            }

            await builder.Patch(_tempApkPath, _logger, _cts.Token);

            // Move the current APK back to the latest complete
            File.Delete(_latestCompleteApkPath);
            File.Move(_tempApkPath, _latestCompleteApkPath);

            // Trigger the next stage
            UpdateStatusPostPatching(await _beatSaberService.GetInstallationInfoAsync(), true);
            _logger.Information("Patching complete");
        }
        catch (OperationCanceledException)
        {
            Log.Information("Patching canceled");
        }
        finally
        {
            EndSetupStage();
        }
    }

    /// <summary>
    /// Fixes the post-patching setup status.
    /// Useful if BS is installed/uninstalled - the status may have to be rolled back to uninstalling vanilla BS,
    /// or rolled forward to finalizing - depending on the new app
    /// </summary>
    /// <param name="installationInfo">The new Beat Saber install</param>
    /// <param name="force">Whether or not to update the status even if not in a post-patch stage</param>
    private void UpdateStatusPostPatching(InstallationInfo? installationInfo, bool force = false)
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
                ProcessStatusChange();
                _logger.Information("Beat Saber was uninstalled");
            }
        }   else if (installationInfo.ModTag == null)
        {
            // Non-modded Beat Saber was installed, so we need to uninstall it
            if (CurrentStatus.Stage != SetupStage.UninstallingOriginal)
            {
                CurrentStatus.Stage = SetupStage.UninstallingOriginal;
                ProcessStatusChange();
                _logger.Information("Unmodded Beat Saber was installed. It will have to be uninstalled");
            }
        }
        else
        {
            if (CurrentStatus.Stage != SetupStage.Finalizing)
            {
                CurrentStatus.Stage = SetupStage.Finalizing;
                ProcessStatusChange();
                _logger.Information("Modded Beat Saber was installed (woohoo)");
            }
        }
    }

    public async Task TriggerUninstallAsync()
    {
        await BeginSetupStage(SetupStage.UninstallingOriginal);
        try
        {
            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
                foreach (string fileName in DataFiles)
                {
                    _logger.Information($"Backing up {fileName}");
                    var filePath = Path.Combine(BeatSaberDataPath, fileName);
                    var backupFilePath = Path.Combine(_backupPath, fileName);

                    if (File.Exists(filePath))
                    {
                        File.Copy(filePath, backupFilePath);
                    }
                }
            }
                
            _beatSaberService.TriggerUninstall();
        }
        finally
        {
            EndSetupStage();
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
            EndSetupStage();
        }
    }

    public async Task FinalizeSetup()
    {
        await BeginSetupStage(SetupStage.Finalizing);
        try
        {
            if (Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(BeatSaberDataPath);
                var files = Directory.GetFiles(_backupPath);
                _logger.Information($"Restoring {files.Length} files");
                foreach (string file in files)
                {
                    var fileName = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(BeatSaberDataPath, fileName));
                }
            }
            else
            {
                _logger.Warning("Could not find backup to restore");
            }
                
            // TODO: Install core mods, etc
            // Install a song by default?
                
            QuitSetupInternal();
            SetupComplete?.Invoke(this, EventArgs.Empty);
            Log.Information("Setup finished");
            // All done!
        }
        finally
        {
            EndSetupStage();
        }
    }
        
    private void OnBeatSaberServiceAppChanged(object? sender, InstallationInfo? installationInfo)
    {
        if (CurrentStatus == null) return;
            
        UpdateStatusPostPatching(installationInfo);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
            
        if (CurrentStatus is { IsInProgress: true })
        {
            Log.Information("Waiting for setup stage to finish");
            _cts.Cancel();
            Thread.Sleep(5000);

            if (CurrentStatus.IsInProgress)
            {
                Log.Warning("Setup stage took too long to shut down");
            }
        }
        _cts.Dispose();
        _stageBeginLock.Dispose();
    }
}