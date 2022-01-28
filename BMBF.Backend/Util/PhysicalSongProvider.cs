using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

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

    public async Task CopyToAsync(string path, IFileSystem fileSystem)
    {
        foreach (string file in _io.Directory.EnumerateFiles(_directoryName, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_directoryName, file);
            string newPath = Path.Combine(path, relativePath);
            var directory = Path.GetDirectoryName(newPath);
            if(directory != null) fileSystem.Directory.CreateDirectory(directory);

            if (fileSystem.File.Exists(newPath)) fileSystem.File.Delete(newPath);

            await using var existingFile = Open(relativePath);
            await using var newFile = fileSystem.File.OpenWrite(newPath);
            await existingFile.CopyToAsync(newFile);
        }
    }
}