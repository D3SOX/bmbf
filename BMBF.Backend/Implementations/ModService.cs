﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
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
    
    private bool _disposed;

    private readonly string _modsPath;

    public ModService(BMBFSettings bmbfSettings)
    {
        _modsPath = Path.Combine(bmbfSettings.RootDataPath, bmbfSettings.ModsDirectoryName);
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
        if (_modsById != null) return _modsById;
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
                    Log.Debug($"No provider found to import {fileName} as mod");
                    return null; // File cannot be loaded as a mod
                }

                var (provider, tempMod) = providerPair.Value;
                Log.Information($"Successfully parsed mod from {fileName}. ID: {tempMod.Id}. Version: {tempMod.Version}. PackageVersion: {tempMod.PackageVersion}");
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
            await LoadNewModsAsyncInternal(_modsById);
        }
        finally
        {
            _installLock.Release();
        }
    }

    async Task<IMod> IModManager.ImportMod(IModProvider provider, Stream stream, string fileName)
    {
        await using var modStream = stream;
        
        var tempMod = await provider.TryParseModAsync(modStream, true);
        if (tempMod == null)
        {
            throw new InstallationException($"Could not parse mod {fileName}");
        }
        tempMod.Dispose();

        modStream.Position = 0;
        return await CacheAndImportMod(provider, modStream, fileName);
    }
    
    private async Task<ModsCache> GetCacheAsyncInternal()
    {
        if (_modsById != null) return _modsById;
            
        var newCache = new ModsCache();
        await LoadNewModsAsyncInternal(newCache);
        _modsById = newCache;
        return _modsById;
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
        while (File.Exists(savePath))
        {
            savePath = Path.Combine(_modsPath, $"{fileNameWithoutExtension}_{i}.{extension}");
            i++;
        }

        Log.Debug($"Caching mod locally to {savePath}");
        var cacheStream = File.Open(savePath, FileMode.Create, FileAccess.ReadWrite);
        IMod? cachedMod = null;
        try
        {
            await modStream.CopyToAsync(cacheStream); // Copy our mod to the mod file on disk
            cacheStream.Position = 0; // Move back to the beginning in order to reparse and add the mod
            
            Log.Debug("Reparsing and adding mod");
            
            cachedMod = await provider.TryParseModAsync(cacheStream);
            if (cachedMod == null)
                throw new InstallationException("Cached mod was null after previously successfully parsing mod");
            
            await AddModAsync(savePath, cachedMod, provider, modsById);
            return cachedMod;
        }
        catch (Exception)
        {
            // If the mod failed to reparse or adding to the provider failed, then ownership is NOT passed to the provider
            // Because of this, we will dispose of the mod and stream now
            cachedMod?.Dispose();
            await cacheStream.DisposeAsync();
            File.Delete(savePath);
            throw;
        }
    }

    private async Task LoadNewModsAsyncInternal(ModsCache cacheById)
    {
        if (!Directory.Exists(_modsPath)) Directory.CreateDirectory(_modsPath);
        
        foreach (string modPath in Directory.EnumerateFiles(_modsPath))
        {
            if (cacheById.Any(pair => pair.Value.path == modPath)) continue;

            Stream? modStream = null;
            IMod? parsedMod = null;
            try
            {
                modStream = File.OpenRead(modPath);
                 
                var result = await FindProvider(modStream, Path.GetFileName(modPath), false);
                if (result is null)
                {
                    Log.Warning($"{Path.GetFileName(modPath)} couldn't be loaded as a mod");
                    // TODO: Delete invalid mod?
                    continue;
                }

                parsedMod = result.Value.mod;
                await AddModAsync(modPath, parsedMod, result.Value.provider, cacheById);
            }
            catch (Exception ex)
            {
                // In this case, ownership is NOT passed to the provider, so we dispose the mod and underlying stream
                if (modStream != null) await modStream.DisposeAsync();
                parsedMod?.Dispose();
                Log.Error(ex, $"Failed to load mod from {modPath}");
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
        foreach (IModProvider provider in selectedProviders)
        {
            var tempMod = await provider.TryParseModAsync(stream, leaveOpen);
            if (tempMod == null) continue;

            return (provider, tempMod);
        }

        return null;
    }

    private async Task AddModAsync(string savePath, IMod cachedMod, IModProvider provider, ModsCache modsById)
    {
        await provider.AddModAsync(cachedMod);
        modsById[cachedMod.Id] = (cachedMod, savePath);
        ModAdded?.Invoke(this, cachedMod);
        
        Log.Information($"Successfully added {cachedMod.Id} v{cachedMod.Version}");
    }

    private void OnModUnloaded(object? sender, string modId)
    {
        if (_modsById == null) return;
        
        if (_modsById.Remove(modId, out var removedMod))
        {
            Log.Information($"Mod {modId} removed - deleting {removedMod.path}");
            File.Delete(removedMod.path);
            ModRemoved?.Invoke(this, modId);
        }
    }

    private void OnModStatusChanged(object? sender, IMod mod)
    {
        ModStatusChanged?.Invoke(this, mod);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose the mod providers, which will also dispose the underlying mods
        foreach (var provider in _modProviders)
        {
            provider.Dispose();
        }
        
        _installLock.Dispose();
    }
}