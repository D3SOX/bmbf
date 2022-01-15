using System;
using System.IO;
using Android.App;
using Android.Content.Res;
using BMBF.Backend;

namespace BMBF.Implementations;

public class AndroidAssetProvider : IAssetProvider
{
    private readonly AssetManager _assetManager;

    public AndroidAssetProvider(Service bmbfService)
    {
        _assetManager = bmbfService.Assets ?? throw new NullReferenceException(nameof(bmbfService.Assets));
    }
    
    public Stream Open(string path)
    {
        return _assetManager.Open(path);
    }
}