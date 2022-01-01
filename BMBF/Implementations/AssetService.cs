using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Android.App;
using Android.Content.Res;
using BMBF.Extensions;
using BMBF.Models.Setup;
using BMBF.Resources;
using BMBF.Services;
using Serilog;
using UnityIndex = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using CoreModsIndex = System.Collections.Generic.Dictionary<string, BMBF.Resources.CoreMods>;

namespace BMBF.Implementations
{
    public class AssetService : IAssetService
    {
        private const string PatchingAssetsPath = "patching";
        
        private readonly AssetManager _assetManager;
        private readonly BuiltInAssets _builtInAssets;
        private readonly HttpClient _httpClient;
        private readonly ResourceUris _resourceUris;
        private readonly string _packageId;

        private List<DiffInfo>? _cachedDiffs;
        private CoreModsIndex? _cachedCoreMods;

        public string? BuiltInAssetsVersion => _builtInAssets.BeatSaberVersion;
        
        public AssetService(Service bmbfService, HttpClient httpClient, BMBFSettings bmbfSettings)
        {
            _assetManager = bmbfService.Assets ?? throw new NullReferenceException("Asset manager was null");
            _httpClient = httpClient;

            using var patchingIndexStream = OpenAsset("patching_assets.json");
            _builtInAssets = patchingIndexStream.ReadAsCamelCaseJson<BuiltInAssets>();

            _resourceUris = bmbfSettings.Resources;
            _packageId = bmbfSettings.PackageId;
        }

        private Stream OpenAsset(string path)
        {
            return _assetManager.Open(path) ?? throw new NullReferenceException(nameof(Stream));
        }

        private async Task<MemoryStream> DownloadToMemoryStream(Uri uri)
        {
            using var resp = await _httpClient.GetAsync(uri);
            resp.EnsureSuccessStatusCode();
            var memStream = new MemoryStream();
            await resp.Content.CopyToAsync(memStream);
            return memStream;
        }
        
        private async Task<T> DownloadJson<T>(Uri uri)
        {
            using var resp = await _httpClient.GetAsync(uri);
            resp.EnsureSuccessStatusCode();
            await using var respStream = await resp.Content.ReadAsStreamAsync();
            return respStream.ReadAsCamelCaseJson<T>();
        }

        public async Task<CoreModsIndex> GetCoreMods(bool refresh)
        {
            try
            {
                if (_cachedCoreMods == null || refresh)
                {
                    Log.Information($"Downloading core mods index from {_resourceUris.CoreModsIndex}");
                    _cachedCoreMods = await DownloadJson<CoreModsIndex>(_resourceUris.CoreModsIndex);
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
                        new CoreMods(DateTime.MinValue, _builtInAssets.CoreMods)
                    }
                };
            }
        }

        public async Task ExtractOrDownloadCoreMod(CoreMod coreMod, string path)
        {
            await using var outputStream = File.OpenWrite(path);
            if (_builtInAssets.CoreMods?.Contains(coreMod) ?? false)
            {
                Log.Information($"Extracting inbuilt core mod {coreMod.FileName}");
                await using var modStream = OpenAsset(Path.Combine("core_mods", coreMod.FileName));
                await modStream.CopyToAsync(outputStream);
            }
            else
            {
                Log.Information($"Downloading core mod {coreMod.FileName}");
                using var resp = await _httpClient.GetAsync(coreMod.DownloadLink);
                await resp.Content.CopyToAsync(outputStream);
            }
        }

        public async Task<Stream> GetDelta(DiffInfo diffInfo)
        {
            return await DownloadToMemoryStream(new Uri(string.Format(_resourceUris.DeltaVersionTemplate, diffInfo.Name)));
        }

        public async Task<List<DiffInfo>> GetDiffs(bool refresh)
        {
            if (refresh || _cachedDiffs == null)
            {
                _cachedDiffs = await DownloadJson<List<DiffInfo>>(_resourceUris.DeltaIndex);
            }
            return _cachedDiffs;
        }

        private (Stream modloader, Stream main) OpenBuiltInModloader(bool is64Bit)
        {
            var modloaderPath = Path.Combine(PatchingAssetsPath, is64Bit ? "libmodloader64.so" : "libmodloader32.so");
            var mainPath = Path.Combine(PatchingAssetsPath, is64Bit ? "libmain64.so" : "libmain32.so");
            return (OpenAsset(modloaderPath), OpenAsset(mainPath));
        }

        public async Task<(Stream modloader, Stream main)> GetModLoader(bool is64Bit)
        {
            try
            {
                var modLoaderVersion = await DownloadJson<ModLoaderVersion>(_resourceUris.ModLoaderVersion);
                // If our inbuilt modloader is the same as the latest in resources, we can also skip downloading
                if (modLoaderVersion.Version == _builtInAssets.ModLoaderVersion)
                {
                    Log.Information($"Latest modloader (v{modLoaderVersion.Version}), matches inbuilt version. Using inbuilt");
                    return OpenBuiltInModloader(is64Bit);
                }
                
                Log.Information("Downloading modloader");
                // TODO: This downloads to a MemoryStream, which is unnecessary but does reduce the headache of disposing the HttpResponseMessage
                return (
                    await DownloadToMemoryStream(is64Bit ? modLoaderVersion.ModLoader64 : modLoaderVersion.ModLoader32),
                    await DownloadToMemoryStream(is64Bit ? modLoaderVersion.Main64 : modLoaderVersion.Main32)
                );
            }
            catch (Exception)
            {
                // Use the inbuilt modloader if no internet
                Log.Warning($"Could not download modloader - using builtin version (v{_builtInAssets.ModLoaderVersion})");
                return OpenBuiltInModloader(is64Bit);
            }
        }

        public async Task<Stream?> GetLibUnity(string beatSaberVersion)
        {
            if (beatSaberVersion == _builtInAssets.BeatSaberVersion)
            {
                Log.Information($"Using built-in libunity.so for Beat Saber v{beatSaberVersion}");
                return OpenAsset(Path.Combine(PatchingAssetsPath, "libunity.so"));
            }

            var unityIndex = await DownloadJson<UnityIndex>(_resourceUris.LibUnityIndex);
            if (!unityIndex.TryGetValue(_packageId, out var unityVersions))
            {
                Log.Warning($"Unity index did not contain any versions for the package ID {_packageId}");
                return null;
            }

            if (unityVersions.TryGetValue(beatSaberVersion, out var libUnityVersion))
            {
                Log.Information($"Downloading libunity.so for Beat Saber v{beatSaberVersion}");
                return await DownloadToMemoryStream(new Uri(string.Format(_resourceUris.LibUnityVersionTemplate, libUnityVersion)));
            }

            Log.Warning($"No libunity version found for {_packageId} v{beatSaberVersion}");
            return null;
        }
    }
}