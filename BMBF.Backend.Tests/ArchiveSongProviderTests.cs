using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Threading.Tasks;
using BMBF.Backend.Util;
using Xunit;

namespace BMBF.Backend.Tests;

public class ArchiveSongProviderTests : IDisposable
{
    private readonly ArchiveSongProvider _provider;
    private readonly ZipArchive _archive;

    private readonly IFileSystem _fileSystem = Util.CreateMockFileSystem();

    public ArchiveSongProviderTests()
    {
        _archive = new ZipArchive(new MemoryStream(), ZipArchiveMode.Update);
        _provider = new ArchiveSongProvider(_archive);
    }

    [Fact]
    public void FileShouldExist()
    {
        _archive.CreateEntry("example.txt");
        Assert.True(_provider.Exists("example.txt"));
    }

    [Fact]
    public void FileShouldNotExist()
    {
        Assert.False(_provider.Exists("example.txt"));
    }

    [Fact]
    public void FileContentShouldMatch()
    {
        var entry = _archive.CreateEntry("example.txt");
        using (var entryStream = entry.Open())
        {
            entryStream.Write(Util.ExampleFileContent);
        }

        using (var entryStream = _provider.Open("example.txt"))
        {
            Util.AssertIsExampleContent(entryStream);
        }
    }

    [Fact]
    public async Task ShouldExtractFiles()
    {
        var entry = _archive.CreateEntry("example.txt");
        await using (var entryStream = entry.Open())
        {
            entryStream.Write(Util.ExampleFileContent);
        }

        await _provider.CopyToAsync("/ExtractPath", _fileSystem);
        Assert.Equal(Util.ExampleFileContent, _fileSystem.File.ReadAllBytes("/ExtractPath/example.txt"));
    }

    [Fact]
    public async Task ShouldOverwriteFiles()
    {
        var entry = _archive.CreateEntry("example.txt");
        await using (var entryStream = entry.Open())
        {
            entryStream.Write(Util.ExampleFileContent);
        }

        _fileSystem.Directory.CreateDirectory("/ExtractPath");

        // We write a longer content than the overwriting content
        // This is to check that the file is deleted and recreated - instead of just the initial bytes being overwritten,
        // with corrupt file content left on the end
        _fileSystem.File.WriteAllBytes("/ExtractPath/example.txt",
            new byte[Util.ExampleFileContent.Length + 10]);

        await _provider.CopyToAsync("/ExtractPath", _fileSystem);
        Assert.Equal(Util.ExampleFileContent, _fileSystem.File.ReadAllBytes("/ExtractPath/example.txt"));
    }


    public void Dispose()
    {
        _archive.Dispose();
    }
}
