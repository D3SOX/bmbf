using System.IO;
using System.IO.Compression;

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
}