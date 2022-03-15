using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.ModManagement;
using BMBF.Resources;
using Serilog;

namespace BMBF.Backend.Implementations;

public class CoreModService : ICoreModService, IDisposable
{
    private readonly IAssetService _assetService;
    private readonly IModService _modService;
    private readonly IBeatSaberService _beatSaberService;
    private readonly IProgressService _progressService;

    private Dictionary<string, CoreMods>? _cachedCoreModsIndex;
    private readonly SemaphoreSlim _coreModsLock = new(1);
    private bool _cacheIsFromInternet;

    public CoreModService(IAssetService assetService,
        IModService modService,
        IBeatSaberService beatSaberService, 
        IProgressService progressService)
    {
        _assetService = assetService;
        _modService = modService;
        _beatSaberService = beatSaberService;
        _progressService = progressService;
    }

    public async Task<CoreModInstallResult> InstallAsync(bool refresh)
    {
        await _coreModsLock.WaitAsync();
        try
        {
            var installInfo = await _beatSaberService.GetInstallationInfoAsync();
            if (installInfo == null)
            {
                return new CoreModInstallResult
                {
                    ResultType = CoreModResultType.BeatSaberNotInstalled
                };
            }
            
            var allCoreMods = await GetCacheAsyncInternal(refresh);
            if (allCoreMods == null)
            {
                // No core mods were built-in, and the online index could not be downloaded
                return new CoreModInstallResult
                {
                    ResultType = CoreModResultType.FailedToFetch
                };
            }

            if (!allCoreMods.TryGetValue(installInfo.Version, out var versionedMods))
            {
                return new CoreModInstallResult
                {
                    ResultType = _cacheIsFromInternet
                        ? CoreModResultType.NoneAvailableForVersion
                        : CoreModResultType.NoneBuiltInForVersion
                };
            }

            var result =  await InstallAsyncInternal(versionedMods);
            result.ResultType =
                _cacheIsFromInternet ? CoreModResultType.UsedDownloaded : CoreModResultType.UsedBuiltIn;
            return result;
        }
        finally
        {
            _coreModsLock.Release();
        }
    }
    
    private async Task<Dictionary<string, CoreMods>?> GetCacheAsyncInternal(bool refresh)
    {
        // If the cache has not been fetched yet, or we are refreshing
        if (_cachedCoreModsIndex == null || refresh)
        {
            var possibleCoreMods = await _assetService.GetCoreMods();
            if (possibleCoreMods == null) // Core mods could not be fetched (inbuilt or otherwise)
            {
                return _cachedCoreModsIndex; // Return the existing core mod index, possibly null
            }
            
            var coreMods = possibleCoreMods.Value;
            // Do not overwrite the index if we have a downloaded index already, and the new index could not be downloaded
            if(_cacheIsFromInternet && !coreMods.downloaded)
            {
                return _cachedCoreModsIndex;
            }
            
            // Otherwise, overwrite and use the new index
            _cacheIsFromInternet = coreMods.downloaded;
            _cachedCoreModsIndex = coreMods.coreMods;
        }
        return _cachedCoreModsIndex;
    }

    private async Task<CoreModInstallResult> InstallAsyncInternal(CoreMods versionedMods)
    {
        using var progress = _progressService.CreateChunkedProgress("Installing core mods", versionedMods.Mods.Count);
        
        var existingMods = await _modService.GetModsAsync();
        var result = new CoreModInstallResult();
        
        foreach (var coreMod in versionedMods.Mods)
        {
            bool needDownload = false;
            if (existingMods.TryGetValue(coreMod.Id, out var mod))
            {
                if (mod.mod.Version < coreMod.Version) // Make sure to keep existing core mods up to date
                {
                    Log.Information($"Core mod {coreMod.Id} is out of date (latest version: " +
                                    $"{coreMod.Version}, current version: {mod.mod.Version})");
                    needDownload = true;
                }
                else
                {
                    // NOTE: We deliberately will not downgrade a core mod if a newer version is installed
                    // This is for the convenience of core mod developers
                    Log.Debug($"Core mod {coreMod.Id} v{coreMod.Version} was already installed");
                }
            }
            else
            {
                Log.Information($"Core mod {coreMod.Id} did not exist");
                needDownload = true;
            }

            IMod? importedMod;
            if (needDownload)
            {
                try
                {
                    // A seekable stream is required for mod imports
                    using var coreModStream = await _assetService.ExtractOrDownloadCoreMod(coreMod);
                    using var tempStream = new MemoryStream();
                    await coreModStream.CopyToAsync(tempStream);
                    tempStream.Position = 0;

                    var importResult =
                        await _modService.TryImportModAsync(tempStream, coreMod.FileName);
                    if (importResult == null || importResult.Type == FileImportResultType.Failed)
                    {
                        Log.Error($"The downloaded mod could not be imported: {importResult?.Error}");
                        result.FailedToInstall.Add(coreMod);
                        continue;
                    }
                    
                    importedMod = importResult.ImportedMod!;
                    result.Added.Add(coreMod);
                }
                catch (HttpRequestException ex)
                {
                    result.FailedToFetch.Add(coreMod);
                    Log.Error(ex, $"Core mod {coreMod.Id} was not built-in, and downloading it failed");
                    continue;
                }
                catch (Exception ex)
                {
                    result.FailedToInstall.Add(coreMod);
                    Log.Error(ex, $"An unknown error occured while downloading/importing {coreMod.Id}");
                    continue;
                }
            }
            else
            {
                importedMod = mod.mod;
            }

            // Existing core mods which are not installed need to be reinstalled
            if (!importedMod.Installed)
            {
                Log.Debug($"Installing core mod {importedMod.Id}");
                try
                {
                    await importedMod.InstallAsync();
                    result.Installed.Add(coreMod);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to reinstall uninstalled core mod {importedMod}");
                    result.FailedToInstall.Add(coreMod);
                }
            }
            progress.ItemsCompleted++;
        }

        return result;
    }
    

    public void Dispose()
    {
        _coreModsLock.Dispose();
    }
}
