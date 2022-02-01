using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models.Setup;
using BMBF.Backend.Services;
using BMBF.Resources;
using Microsoft.Extensions.FileProviders;
using Serilog;
using UnityIndex = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using CoreModsIndex = System.Collections.Generic.Dictionary<string, BMBF.Resources.CoreMods>;
using Version = SemanticVersioning.Version;

namespace BMBF.Backend.Implementations;

public class AssetService : IAssetService
{
    #region Asset Paths

    internal const string IndexPath = "patching_assets.json";
    internal const string ExtensionsPath = "extensions.json";
    internal const string PatchingAssetsFolder = "patching";
    internal const string CoreModsFolder = "core_mods";
    
    // These are deliberately NOT suffixed with .so !
    // This is because any SO files in the xamarin assets folder are always treated as native libraries
    // and are copied to the libs folder within the APK.
    // Removing the .so suffix gets round this.
    internal const string ModLoader32Path = $"{PatchingAssetsFolder}/libmodloader32";
    internal const string Main32Path = $"{PatchingAssetsFolder}/libmain32";
    internal const string ModLoader64Path = $"{PatchingAssetsFolder}/libmodloader64";
    internal const string Main64Path = $"{PatchingAssetsFolder}/libmain64";

    internal const string UnityPath = $"{PatchingAssetsFolder}/libunity";

    #endregion
    
    private readonly BuiltInAssets _builtInAssets;
    private readonly HttpClient _httpClient;
    private readonly BMBFResources _bmbfResources;
    private readonly string _packageId;

    private readonly IFileProvider _assetProvider;


    private List<DiffInfo>? _cachedDiffs;
    private CoreModsIndex? _cachedCoreMods;

    public string? BuiltInAssetsVersion => _builtInAssets.BeatSaberVersion;

    public AssetService(IFileProvider assetProvider, HttpClient httpClient, BMBFSettings bmbfSettings, BMBFResources bmbfResources)
    {
        _assetProvider = assetProvider;
        _httpClient = httpClient;

        var indexFile = assetProvider.GetFileInfo(IndexPath);
        if (indexFile.Exists)
        {
            using var indexStream = indexFile.CreateReadStream();
            _builtInAssets = indexStream.ReadAsCamelCaseJson<BuiltInAssets>();
        }
        else
        {
            _builtInAssets = new BuiltInAssets();
        }

        _bmbfResources = bmbfResources;
        _packageId = bmbfSettings.PackageId;
    }

    private Stream OpenAsset(string path)
    {
        return _assetProvider.GetFileInfo(path).CreateReadStream();
    }

    private async Task<T> DownloadJson<T>(Uri uri)
    {
        using var resp = await _httpClient.GetStreamAsync(uri);
        return await resp.ReadAsCamelCaseJsonAsync<T>();
    }

    public async Task<CoreModsIndex> GetCoreMods(bool refresh)
    {
        try
        {
            if (_cachedCoreMods == null || refresh)
            {
                Log.Information($"Downloading core mods index from {_bmbfResources.CoreModsIndex}");
                _cachedCoreMods = await DownloadJson<CoreModsIndex>(_bmbfResources.CoreModsIndex);
            }

            return _cachedCoreMods;
        }
        catch (Exception ex)
        {
            if (_builtInAssets.CoreMods == null || _builtInAssets.BeatSaberVersion == null)
            {
                Log.Error(ex, "Failed to download core mods, and no core mods were built in");
                return new CoreModsIndex();
            }

            Log.Warning(ex, "Failed to download core mods - using inbuilt core mods");
            return new CoreModsIndex
            {
                {
                    _builtInAssets.BeatSaberVersion,
                    new CoreMods(DateTime.MinValue.ToString(CultureInfo.InvariantCulture), _builtInAssets.CoreMods)
                }
            };
        }
    }

    public async Task<Stream> ExtractOrDownloadCoreMod(CoreMod coreMod)
    {
        if (_builtInAssets.CoreMods?.Any(mod => mod.Version == coreMod.Version && mod.Id == coreMod.Id) ?? false)
        {
            Log.Information($"Extracting inbuilt core mod {coreMod.FileName}");
            return OpenAsset(Path.Combine(CoreModsFolder, coreMod.FileName));
        }

        Log.Information($"Downloading core mod {coreMod.FileName}");
        return await _httpClient.GetStreamAsync(coreMod.DownloadLink);
    }

    public async Task<Stream> GetDelta(DiffInfo diffInfo, CancellationToken ct)
    {
        var uri = new Uri(string.Format(_bmbfResources.DeltaVersionTemplate, diffInfo.Name));
        var resultStream = new MemoryStream();
        try
        {
            // Diffs must be seekable for use with octodiff, so we copy to a MemoryStream
            await using var respStream =  await _httpClient.GetStreamAsync(uri, ct);
            await respStream.CopyToAsync(resultStream, ct);
            resultStream.Position = 0;

            return respStream;
        }
        catch (Exception)
        {
            // In the case that downloading the delta fails, we must dispose the MemoryStream
            // Since ownership is NOT passed to the caller
            await resultStream.DisposeAsync();
            throw;
        }
    }

    public async Task<List<DiffInfo>> GetDiffs(bool refresh)
    {
        if (refresh || _cachedDiffs == null)
        {
            _cachedDiffs = await DownloadJson<List<DiffInfo>>(_bmbfResources.DeltaIndex);
        }
        return _cachedDiffs;
    }

    private (Stream modloader, Stream main, Version version) OpenBuiltInModloader(bool is64Bit)
    {
        var modloaderPath = is64Bit ? ModLoader64Path : ModLoader32Path;
        var mainPath = is64Bit ? Main64Path : Main32Path;
        return (OpenAsset(modloaderPath), OpenAsset(mainPath), Version.Parse(_builtInAssets.ModLoaderVersion));
    }

    public async Task<(Stream modloader, Stream main, Version version)> GetModLoader(bool is64Bit, CancellationToken ct)
    {
        try
        {
            var modLoaderVersion = await DownloadJson<ModLoaderVersion>(_bmbfResources.ModLoaderVersion);
            // If our inbuilt modloader is the same as the latest in resources, we can also skip downloading
            if (modLoaderVersion.Version == _builtInAssets.ModLoaderVersion)
            {
                Log.Information($"Latest modloader (v{modLoaderVersion.Version}), matches inbuilt version. Using inbuilt");
                return OpenBuiltInModloader(is64Bit);
            }

            Log.Information("Downloading modloader");
            return (
                await _httpClient.GetStreamAsync(is64Bit ? modLoaderVersion.ModLoader64 : modLoaderVersion.ModLoader32, ct),
                await _httpClient.GetStreamAsync(is64Bit ? modLoaderVersion.Main64 : modLoaderVersion.Main32, ct),
                Version.Parse(modLoaderVersion.Version)
            );
        }
        catch (Exception)
        {
            // Use the inbuilt modloader if no internet
            if (_builtInAssets.ModLoaderVersion == null)
            {
                Log.Error("Downloading modloader failed, and no version was built in!");
                throw;
            }

            Log.Warning($"Could not download modloader - using builtin version (v{_builtInAssets.ModLoaderVersion})");
            return OpenBuiltInModloader(is64Bit);
        }
    }

    public async Task<Stream?> GetLibUnity(string beatSaberVersion, CancellationToken ct)
    {
        if (beatSaberVersion == _builtInAssets.BeatSaberVersion)
        {
            Log.Information($"Using built-in libunity.so for Beat Saber v{beatSaberVersion}");
            return OpenAsset(UnityPath);
        }

        var unityIndex = await DownloadJson<UnityIndex>(_bmbfResources.LibUnityIndex);
        if (!unityIndex.TryGetValue(_packageId, out var unityVersions))
        {
            Log.Warning($"Unity index did not contain any versions for the package ID {_packageId}");
            return null;
        }

        if (unityVersions.TryGetValue(beatSaberVersion, out var libUnityVersion))
        {
            Log.Information($"Downloading libunity.so for Beat Saber v{beatSaberVersion}");
            var uri = new Uri(string.Format(_bmbfResources.LibUnityVersionTemplate, libUnityVersion));
            return await _httpClient.GetStreamAsync(uri, ct);
        }

        Log.Warning($"No libunity version found for {_packageId} v{beatSaberVersion}");
        return null;
    }

    public async Task<FileExtensions> GetExtensions()
    {
        try
        {
            return await DownloadJson<FileExtensions>(_bmbfResources.ExtensionsIndex);
        }
        catch (Exception)
        {
            var extensionsFile = _assetProvider.GetFileInfo(ExtensionsPath);
            if (extensionsFile.Exists)
            {
                Log.Warning("Could not fetch extensions from BMBF resources, using built in extensions instead!");

                await using var extensionsStream = extensionsFile.CreateReadStream();
                return extensionsStream.ReadAsCamelCaseJson<FileExtensions>();
            }

            throw;
        }
    }
}
