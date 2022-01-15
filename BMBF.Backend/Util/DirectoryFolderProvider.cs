using System.IO;

namespace BMBF.Util;

/// <summary>
/// Folder provider using an actual directory on disk
/// </summary>
public class DirectoryFolderProvider : IFolderProvider
{
    private readonly string _directoryName;
        
    public DirectoryFolderProvider(string directoryName)
    {
        _directoryName = directoryName;
    }
        
    public bool Exists(string name)
    {
        return File.Exists(Path.Combine(_directoryName, name));
    }

    public Stream Open(string name)
    {
        return File.OpenRead(Path.Combine(_directoryName, name));
    }
}