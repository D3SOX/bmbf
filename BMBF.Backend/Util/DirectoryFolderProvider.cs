using System.IO;
using System.IO.Abstractions;

namespace BMBF.Backend.Util;

/// <summary>
/// Folder provider using an actual directory on disk
/// </summary>
public class DirectoryFolderProvider : IFolderProvider
{
    private readonly string _directoryName;
    private readonly IFileSystem _io;
        
    public DirectoryFolderProvider(string directoryName, IFileSystem io)
    {
        _directoryName = directoryName;
        _io = io;
    }
        
    public bool Exists(string name)
    {
        return _io.File.Exists(Path.Combine(_directoryName, name));
    }

    public Stream Open(string name)
    {
        return _io.File.OpenRead(Path.Combine(_directoryName, name));
    }
}