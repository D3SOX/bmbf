using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Threading.Tasks;

namespace BMBF.Backend.Util;

/// <summary>
/// Song provider that uses a ZipArchive
/// </summary>
public class ArchiveSongProvider : ISongProvider
{
    private readonly ZipArchive _archive;

    public ArchiveSongProvider(ZipArchive archive)
    {
        _archive = archive;
    }

    public bool Exists(string name)
    {
        return _archive.GetEntry(name) != null;
    }

    public Stream Open(string name)
    {
        return (_archive.GetEntry(name) ?? throw new FileNotFoundException("No file existed in the folder called " + name)).Open();
    }

    public async Task CopyToAsync(string path, IFileSystem fileSystem)
    {
        foreach (var entry in _archive.Entries)
        {
            string newPath = Path.Combine(path, entry.FullName);
            var directory = Path.GetDirectoryName(newPath);
            if (directory != null) fileSystem.Directory.CreateDirectory(directory);

            if (fileSystem.File.Exists(newPath)) fileSystem.File.Delete(newPath);

            await using var existingFile = entry.Open();
            await using var newFile = fileSystem.File.OpenWrite(newPath);
            await existingFile.CopyToAsync(newFile);
        }
    }
}