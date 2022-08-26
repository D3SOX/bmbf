using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using BMBF.Desktop.Configuration;
using BMBF.Patching;
using QuestPatcher.Axml;
using Serilog;

namespace BMBF.Desktop.Implementations;

public class BeatSaberService : IBeatSaberService, IDisposable
{
    public event EventHandler<InstallationInfo?>? AppChanged;

    private readonly FileSystemWatcher _apkWatcher = new();
    private readonly ITagManager _tagManager;

    private readonly SemaphoreSlim _appInfoLoadLock = new(1);
    private readonly Debouncey _appUpdateDebouncey = new(3000);

    private bool _disposed;
    private InstallationInfo? _installationInfo;

    private bool _loadedInitialInstallation;

    private readonly string _apkPath;

    public BeatSaberService(BMBFDesktopSettings desktopSettings, ITagManager tagManager)
    {
        _tagManager = tagManager;
        _apkPath = Path.Combine(desktopSettings.DeviceRoot, desktopSettings.ApkPath);
        _appUpdateDebouncey.Debounced += OnAppUpdateDebounced;
    }

    private void StartWatching()
    {
        var apkDirectory = Path.GetDirectoryName(_apkPath);
        if (apkDirectory == null) throw new DirectoryNotFoundException("APK was not in a directory");

        _apkWatcher.Path = apkDirectory;
        _apkWatcher.Filter = Path.GetFileName(_apkPath);
        _apkWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        _apkWatcher.Deleted += OnApkDeleted;
        _apkWatcher.Changed += OnApkChanged;
        _apkWatcher.Renamed += OnApkRenamed;
        _apkWatcher.EnableRaisingEvents = true;
    }

    public async Task<InstallationInfo?> GetInstallationInfoAsync()
    {
        if (!_loadedInitialInstallation)
        {
            await _appInfoLoadLock.WaitAsync();
            try
            {
                _installationInfo = await LoadInstallationInfoAsync();

                StartWatching();
                _loadedInitialInstallation = true;
            }
            finally
            {
                _appInfoLoadLock.Release();
            }
        }

        return _installationInfo;
    }

    private async Task<InstallationInfo?> LoadInstallationInfoAsync()
    {
        if (!File.Exists(_apkPath)) return null;

        await using var apkStream = File.OpenRead(_apkPath);
        using var apkArchive = new ZipArchive(apkStream, ZipArchiveMode.Read);

        await using var manifestStream = apkArchive.GetEntry("AndroidManifest.xml")?.Open();
        if (manifestStream == null)
        {
            Log.Error("No manifest existed in APK");
            return null;
        }

        await using var tempStream = new MemoryStream();
        await manifestStream.CopyToAsync(tempStream);
        tempStream.Position = 0;

        // Load the manifest in order to fetch the APK version
        var manifest = AxmlLoader.LoadDocument(tempStream);
        int versionCode = (int) manifest.Attributes.Single(attr => attr.Name == "versionCode").Value;
        string versionName = (string) manifest.Attributes.Single(attr => attr.Name == "versionName").Value;

        // Fetch the APK to tag (checking if modded)
        var tag = _tagManager.GetTag(apkArchive);

        return new InstallationInfo(versionName, versionCode, tag, _apkPath);
    }

    private async void OnAppUpdateDebounced(object? sender, EventArgs args)
    {
        await _appInfoLoadLock.WaitAsync();
        try
        {
            var newInstall = await LoadInstallationInfoAsync();
            _installationInfo = newInstall;
            AppChanged?.Invoke(this, newInstall);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process app info change - assuming that Beat Saber is not installed");
            _installationInfo = null;
        }
        finally
        {
            _appInfoLoadLock.Release();
        }
    }

    private void OnApkDeleted(object? sender, FileSystemEventArgs args) => _appUpdateDebouncey.Invoke();

    private void OnApkChanged(object? sender, FileSystemEventArgs args) => _appUpdateDebouncey.Invoke();

    private void OnApkRenamed(object? sender, FileSystemEventArgs args) => _appUpdateDebouncey.Invoke();

    public void TriggerInstall(string apkPath)
    {
        File.Copy(apkPath, _apkPath);
    }

    public void TriggerUninstall()
    {
        File.Delete(_apkPath);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _apkWatcher.Dispose();
        _appUpdateDebouncey.Dispose();
        _appInfoLoadLock.Dispose();
    }

    public void Launch()
    {
        Log.Warning("Beat Saber was requested to be launched, but this cannot happen on the PC wrapper");
    }
}
