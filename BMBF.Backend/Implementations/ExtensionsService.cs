﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;

namespace BMBF.Backend.Implementations;

public class ExtensionsService : IExtensionsService
{
    public ConcurrentDictionary<string, FileCopyInfo> CopyExtensions { get; } =
        new();
    public HashSet<string> PlaylistExtensions { get; private set; } = new();
    public HashSet<string> ConfigExtensions { get; private set; } = new();

    private bool _loadedExtensions;
    private readonly IAssetService _assetService;

    public ExtensionsService(IAssetService assetService)
    {
        _assetService = assetService;
    }
        
    public async ValueTask LoadExtensions()
    {
        if (_loadedExtensions) return;

        var extensions = await _assetService.GetExtensions();
        PlaylistExtensions = extensions.PlaylistExtensions.ToHashSet();
        ConfigExtensions = extensions.ConfigExtensions.ToHashSet();
        foreach (var extension in extensions.CopyExtensions)
        {
            CopyExtensions.TryAdd(extension.Key, new FileCopyInfo(extension.Value, null));
        }

        _loadedExtensions = true;
    }
}