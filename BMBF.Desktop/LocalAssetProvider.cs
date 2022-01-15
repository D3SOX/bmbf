using System.IO;
using BMBF.Backend;
using BMBF.Desktop.Configuration;

namespace BMBF.Desktop;

public class LocalAssetProvider : IAssetProvider
{
    private readonly string _assetsPath;
    
    public LocalAssetProvider(BMBFDesktopSettings desktopSettings)
    {
        _assetsPath = desktopSettings.AssetsPath;
    }


    public Stream Open(string path)
    {
        return File.OpenRead(Path.Combine(_assetsPath, path));
    }
}