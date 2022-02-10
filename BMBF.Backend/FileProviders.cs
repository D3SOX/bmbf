using Microsoft.Extensions.FileProviders;

namespace BMBF.Backend;

public class FileProviders
{
    public FileProviders(IFileProvider assetProvider, IFileProvider webRootProvider)
    {
        AssetProvider = assetProvider;
        WebRootProvider = webRootProvider;
    }

    /// <summary>
    /// File provider for BMBF built-in asset files
    /// </summary>
    public IFileProvider AssetProvider { get; }
    
    /// <summary>
    /// File provider for static web files
    /// </summary>
    public IFileProvider WebRootProvider { get; }
}
