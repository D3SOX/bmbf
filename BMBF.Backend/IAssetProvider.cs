using System.IO;

namespace BMBF.Backend;

public interface IAssetProvider
{
    /// <summary>
    /// Opens the asset with the given path
    /// </summary>
    /// <param name="path">The path of the asset to open</param>
    /// <returns>A stream which can be used to read the asset</returns>
    Stream Open(string path);
}