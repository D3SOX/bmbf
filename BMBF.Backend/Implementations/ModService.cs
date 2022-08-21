using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using BMBF.ModManagement;
using Serilog;
using ModsCache = System.Collections.Concurrent.ConcurrentDictionary<string, (BMBF.ModManagement.IMod mod, string path)>;

namespace BMBF.Backend.Implementations;

public class ModService : IModService, IDisposable, IModManager
{
    public event EventHandler<IMod>? ModAdded;
    public event EventHandler<string>? ModRemoved;
    public event EventHandler<IMod>? ModStatusChanged;

    SemaphoreSlim IModManager.InstallLock => _installLock;
    private readonly SemaphoreSlim _installLock = new(1);

    private readonly List<IModProvider> _modProviders = new();

    private ModsCache? _modsById;

    private readonly string _modsPath;
    private readonly IFileSystem _io;
    private readonly BMBFSettings _bmbfSettings;
    private readonly IFileSystemWatcher _modFilesWatcher;
    private readonly IFileSystemWatcher _libFilesWatcher;
    private readonly ILogger _logger;
    
    private readonly Debouncey _modFilesDebouncey;
    
    private bool _disposed;

    public ModService(BMBFSettings bmbfSettings,
        IFileSystem io,
        IFileSystemWatcher modFilesWatcher,
        IFileSystemWatcher libFilesWatcher)
    {
        _modFilesWatcher = modFilesWatcher;
        _libFilesWatcher = libFilesWatcher;
        _modsPath = Path.Combine(bmbfSettings.RootDataPath, bmbfSettings.ModsDirectoryName);
        _io = io;
        _bmbfSettings = bmbfSettings;
        _logger = Log.Logger.ForContext(LogType.ModInstallation);

        _modFilesDebouncey = new Debouncey(bmbfSettings.ModFilesDebounceDelay);
        _modFilesDebouncey.Debounced += OnModFileDebounced;
    }

    /// <summary>
    /// Adds a provider for use in parsing and adding mods.
    /// </summary>
    /// <param name="provider">Provider to add</param>
    public void RegisterProvider(IModProvider provider)
    {
        provider.ModUnloaded += OnModUnloaded;
        provider.ModStatusChanged += OnModStatusChanged;
        _modProviders.Add(provider);
    }

    public async Task<IReadOnlyDictionary<string, (IMod mod, string path)>> GetModsAsync()
    {
        if (_modsById != null)
        {
            return _modsById;
        }

        await _installLock.WaitAsync();
        try
        {
            return await GetCacheAsyncInternal();
        }
        finally
        {
            _installLock.Release();
        }
    }

    public async Task<FileImportResult?> TryImportModAsync(Stream stream, string fileName)
    {
        await _installLock.WaitAsync();
        try
        {
            IMod mod;
            try
            {
                // Find a provider which can be used to load this mod
                var providerPair = await FindProvider(stream, fileName, true);
                if (providerPair == null)
                {
                    _logger.Debug($"No provider found to import {fileName} as mod");
                    return null; // File cannot be loaded as a mod
                }

                var (provider, tempMod) = providerPair.Value;
                _logger.Debug($"Successfully parsed mod from {fileName}. ID: {tempMod.Id}. Version: {tempMod.Version}. PackageVersion: {tempMod.PackageVersion}");
                tempMod.Dispose();

                // Wind back the stream in order to save, then reimport the mod
                stream.Position = 0;
                mod = await CacheAndImportMod(provider, stream, fileName);
            }
            catch (InstallationException ex)
            {
                return FileImportResult.CreateError(ex.Message);
            }

            return new FileImportResult
            {
                Type = FileImportResultType.Mod,
                ImportedMod = mod
            };
        }
        finally
        {
            _installLock.Release();
        }
    }

    public async Task UnloadModAsync(IMod mod)
    {
        await _installLock.WaitAsync();
        try
        {
            foreach (var provider in _modProviders)
            {
                await provider.UnloadModAsync(mod);
            }
        }
        finally
        {
            _installLock.Release();
        }
    }

    public async Task LoadNewModsAsync()
    {
        if (_modsById == null)
        {
            await GetModsAsync();
            return;
        }

        await _installLock.WaitAsync();
        try
        {
            await LoadNewModsAsyncInternal(_modsById, true);
        }
        finally
        {
            _installLock.Release();
        }
    }

    public async Task UpdateModStatusesAsync()
    {
        await _installLock.WaitAsync();
        try
        {
            foreach (var provider in _modProviders)
            {
                provider.UpdateModStatuses();
            }
        }
        finally
        {
            _installLock.Release();
        }
    }

    async Task<IMod> IModManager.ImportMod(IModProvider provider, Stream stream, string fileName)
    {
        await using var modStream = stream;

        try
        {
            var tempMod = await provider.TryParseModAsync(modStream, true);
            if (tempMod == null)
            {
                throw new InstallationException($"Could not parse mod {fileName}");
            }
            tempMod.Dispose();

            modStream.Position = 0;
            return await CacheAndImportMod(provider, modStream, fileName);
        }
        catch (Exception)
        {
            await modStream.DisposeAsync();
            throw;
        }
    }

    private async Task<ModsCache> GetCacheAsyncInternal()
    {
        if (_modsById != null)
        {
            return _modsById;
        }

        var newCache = new ModsCache();
        await LoadNewModsAsyncInternal(newCache, false);
        _modsById = newCache;

        // Now that we have loaded all mods, we need to start checking for changes in the mod folders (if configured)
        // This guarantees that IMod.Installed is kept up to date
        if (_bmbfSettings.UpdateModStatusesAutomatically)
        {
            StartWatchingForChanges(_modFilesWatcher, _bmbfSettings.ModFilesPath);
            StartWatchingForChanges(_libFilesWatcher, _bmbfSettings.LibFilesPath);
        }
        
        return _modsById;
    }

    private void StartWatchingForChanges(IFileSystemWatcher watcher, string path)
    {
        _io.Directory.CreateDirectory(path);
        watcher.Path = path;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        watcher.Filter = "*.*";
        watcher.Created += OnModFilesUpdated;
        watcher.Deleted += OnModFilesUpdated;
        watcher.Renamed += OnModFilesUpdated;
        watcher.EnableRaisingEvents = true;
    }

    private async Task<IMod> CacheAndImportMod(IModProvider provider, Stream modStream, string fileName)
    {
        // Once we've parsed the mod temporarily, we need to cache it to a local file
        var modsById = await GetCacheAsyncInternal();

        // Find a path to save the mod to
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        string savePath = Path.Combine(_modsPath, fileName);
        int i = 1;
        while (_io.File.Exists(savePath))
        {
            savePath = Path.Combine(_modsPath, $"{fileNameWithoutExtension}_{i}{extension}");
            i++;
        }

        _logger.Debug($"Caching mod locally to {savePath}");
        var cacheStream = _io.File.Open(savePath, FileMode.Create, FileAccess.ReadWrite);
        IMod? cachedMod = null;
        try
        {
            await modStream.CopyToAsync(cacheStream); // Copy our mod to the mod file on disk
            cacheStream.Position = 0; // Move back to the beginning in order to reparse and add the mod

            _logger.Debug("Reparsing and adding mod");

            cachedMod = await provider.TryParseModAsync(cacheStream);
            if (cachedMod == null)
                throw new InstallationException("Cached mod was null after previously successfully parsing mod");

            await AddModAsync(savePath, cachedMod, provider, modsById, true);
            return cachedMod;
        }
        catch (Exception)
        {
            // If the mod failed to reparse or adding to the provider failed, then ownership is NOT passed to the provider
            // Because of this, we will dispose of the mod and stream now
            cachedMod?.Dispose();
            await cacheStream.DisposeAsync();
            _io.File.Delete(savePath);
            throw;
        }
    }

    private async Task LoadNewModsAsyncInternal(ModsCache cacheById, bool notify)
    {
        _io.Directory.CreateDirectory(_modsPath);

        foreach (string modPath in _io.Directory.EnumerateFiles(_modsPath))
        {
            if (cacheById.Any(pair => pair.Value.path == modPath)) continue;

            Stream? modStream = null;
            IMod? parsedMod = null;
            try
            {
                modStream = _io.File.OpenRead(modPath);

                var result = await FindProvider(modStream, Path.GetFileName(modPath), false);
                if (result is not null)
                {
                    parsedMod = result.Value.mod;
                    await AddModAsync(modPath, parsedMod, result.Value.provider, cacheById, notify);
                    continue;
                }
                _logger.Warning($"{Path.GetFileName(modPath)} couldn't be loaded as a mod");
            }
            catch (Exception ex)
            {
                // In this case, ownership is NOT passed to the provider, so we dispose the mod and underlying stream
                if (modStream != null) await modStream.DisposeAsync();
                parsedMod?.Dispose();
                _logger.Error(ex, $"Failed to load mod from {modPath}");
            }

            if (_bmbfSettings.DeleteInvalidMods)
            {
                _logger.Debug("Deleting invalid mod");
                try
                {
                    _io.File.Delete(modPath);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to delete invalid mod");
                }
            }
        }
    }

    private async Task<(IModProvider provider, IMod mod)?> FindProvider(Stream stream, string fileName, bool leaveOpen)
    {
        // Find a provider that can attempt to import a file with this name
        var selectedProviders = new List<IModProvider>();
        foreach (IModProvider provider in _modProviders)
        {
            if (provider.CanAttemptImport(fileName)) selectedProviders.Add(provider);
        }

        // If we cannot find any providers that can import this filename, then we can immediately return an import failure
        if (selectedProviders.Count == 0)
        {
            return null;
        }

        // Attempt to parse a mod with the selected providers
        foreach (var provider in selectedProviders)
        {
            var tempMod = await provider.TryParseModAsync(stream, leaveOpen);
            if (tempMod == null)
            {
                continue;
            }

            return (provider, tempMod);
        }

        return null;
    }

    private async Task AddModAsync(string savePath, IMod cachedMod, IModProvider provider, ModsCache modsById, bool notify)
    {
        await provider.AddModAsync(cachedMod);
        modsById[cachedMod.Id] = (cachedMod, savePath);
        if (notify)
        {
            ModAdded?.Invoke(this, cachedMod);
        }

        _logger.Information($"Successfully added {cachedMod.Id} v{cachedMod.Version}");
    }

    private void OnModUnloaded(object? sender, string modId)
    {
        if (_modsById == null)
        {
            return;
        }

        if (_modsById.Remove(modId, out var removedMod))
        {
            _logger.Information($"Mod {modId} removed - deleting {removedMod.path}");
            _io.File.Delete(removedMod.path);
            ModRemoved?.Invoke(this, modId);
        }
    }

    private void OnModStatusChanged(object? sender, IMod mod)
    {
        _logger.Debug($"Mod {mod.Id} marked as {(mod.Installed ? "installed" : "uninstalled")}");
        ModStatusChanged?.Invoke(this, mod);
    }

    private void OnModFilesUpdated(object? sender, FileSystemEventArgs args) => _modFilesDebouncey.Invoke();

    private async void OnModFileDebounced(object? sender, EventArgs args)
    {
        _logger.Debug("Processing mods/libs debounce");
        try
        {
            await UpdateModStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to process mod/libs folders update");
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        
        _modFilesWatcher.Dispose();
        _libFilesWatcher.Dispose();
        _modFilesDebouncey.Dispose();

        // Dispose the mod providers, which will also dispose the underlying mods
        foreach (var provider in _modProviders)
        {
            provider.Dispose();
        }

        _installLock.Dispose();
    }
}
