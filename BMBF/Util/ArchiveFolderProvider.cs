using System.IO;
using System.IO.Compression;

namespace BMBF.Util;

/// <summary>
/// Folder provider that uses a ZipArchive
/// </summary>
public class ArchiveFolderProvider : IFolderProvider
{
    private readonly ZipArchive _archive;
        
    public ArchiveFolderProvider(ZipArchive archive)
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