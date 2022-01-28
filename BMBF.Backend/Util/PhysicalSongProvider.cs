using System.IO;
using System.IO.Abstractions;

namespace BMBF.Backend.Util;

/// <summary>
/// Song provider using an actual directory on a <see cref="IFileSystem"/>
/// </summary>
public class PhysicalSongProvider : ISongProvider
{
    private readonly string _directoryName;
    private readonly IFileSystem _io;
        
    public PhysicalSongProvider(string directoryName, IFileSystem io)
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