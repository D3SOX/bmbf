using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using BMBF.Backend.Configuration;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Patching;
using Java.Lang;
using Serilog;
using Exception = System.Exception;

namespace BMBF.Implementations;

public class BeatSaberService : BroadcastReceiver, IBeatSaberService, IDisposable
{
    private readonly Service _bmbfService;
    private readonly PackageManager _packageManager;
    private readonly string _packageId;
    private readonly ITagManager _tagManager;

    public event EventHandler<InstallationInfo?>? AppChanged;

    private InstallationInfo? _installationInfo;
    private readonly SemaphoreSlim _appInfoLoadLock = new(1);
    private bool _disposed;

    public BeatSaberService(Service bmbfService, BMBFSettings bmbfSettings, ITagManager tagManager)
    {
        _packageManager = bmbfService.PackageManager ?? throw new NullReferenceException(nameof(bmbfService.PackageManager));
        _packageId = bmbfSettings.PackageId;
        _bmbfService = bmbfService;
        _tagManager = tagManager;

        // Listen for package installs and uninstalls
        IntentFilter intentFilter = new IntentFilter();
        intentFilter.AddAction(Intent.ActionPackageAdded);
        intentFilter.AddAction(Intent.ActionPackageRemoved);
        intentFilter.AddAction(Intent.ActionPackageReplaced);
        intentFilter.AddDataScheme("package");
        _bmbfService.RegisterReceiver(this, intentFilter);
    }

    public async Task<InstallationInfo?> GetInstallationInfoAsync()
    {
        if (_installationInfo == null)
        {
            await _appInfoLoadLock.WaitAsync();
            try
            {
                if (_installationInfo != null) return _installationInfo;
                _installationInfo = await LoadInstallationInfoAsync();
                return _installationInfo;
            }
            finally
            {
                _appInfoLoadLock.Release();
            }
        }
        return _installationInfo;
    }

    public void TriggerInstall(string apkPath)
    {
        Intent intent = new Intent(BMBFIntents.TriggerPackageInstall);
        intent.PutExtra("ApkPath", apkPath);
        _bmbfService.SendBroadcast(intent);
    }

    public void TriggerUninstall()
    {
        Intent intent = new Intent(BMBFIntents.TriggerPackageUninstall);
        intent.PutExtra("PackageId", _packageId);
        _bmbfService.SendBroadcast(intent);
    }

    private async Task<InstallationInfo?> LoadInstallationInfoAsync()
    {
        var packageInfo = _packageManager.GetInstalledPackages(0).FirstOrDefault(package => package.PackageName == _packageId);

        if (packageInfo == null)
        {
            return null;
        }
        var apkPath = packageInfo.ApplicationInfo?.PublicSourceDir;
        if (apkPath is null)
        {
            return null;
        }

        if (!File.Exists(apkPath))
        {
            Log.Warning($"No APK existed for package {_packageId}");
            return null;
        }

        await using var apkStream = File.OpenRead(apkPath);
        using var apkArchive = new ZipArchive(apkStream, ZipArchiveMode.Read);

        var tag = _tagManager.GetTag(apkArchive);
        if (tag == null)
        {
            Log.Information("APK was not modded");
        }
        else
        {
            Log.Information($"APK was modded by {tag.PatcherName} v{tag.PatcherVersion?.ToString() ?? "<unknown version>"} with {tag.ModloaderName ?? "No modloader"} v{tag.ModloaderVersion?.ToString() ?? "<unknown version>"}");
        }
        Log.Information($"APK version name: {packageInfo.VersionName}. version code: {packageInfo.LongVersionCode}");

        return new InstallationInfo(
            packageInfo.VersionName ?? "unknown",
            (int) packageInfo.LongVersionCode,
            tag,
            apkPath
        );
    }

    public override async void OnReceive(Context? context, Intent? intent)
    {
        try
        {
            if (context == null || intent == null) { return; }
            string? packageId = intent.Data?.EncodedSchemeSpecificPart;
            if (packageId != _packageId) { return; }

            if (intent.Action == Intent.ActionPackageReplaced)
            {
                Log.Information($"{_packageId} replaced");
            }

            if (intent.Action == Intent.ActionPackageAdded)
            {
                Log.Information($"{_packageId} installed");
            }
            else if (intent.Action == Intent.ActionPackageRemoved)
            {
                Log.Information($"{_packageId} uninstalled");
            }

            // Load the installation for the new package, then trigger the app changed event
            await _appInfoLoadLock.WaitAsync();
            try
            {
                var newInstall = await LoadInstallationInfoAsync();
                _installationInfo = newInstall;
                AppChanged?.Invoke(this, newInstall);
            }
            finally
            {
                _appInfoLoadLock.Release();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process app install/uninstall");
        }
    }

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _appInfoLoadLock.Dispose();
        base.Dispose();
        try
        {
            _bmbfService.UnregisterReceiver(this);
        }
        catch (IllegalArgumentException)
        {
            // Already unregistered
        }
    }
}
