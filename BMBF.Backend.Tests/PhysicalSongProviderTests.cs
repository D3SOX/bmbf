using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using BMBF.Backend.Util;
using Xunit;

namespace BMBF.Backend.Tests;

public class PhysicalSongProviderTests
{
    private readonly PhysicalSongProvider _provider;

    private readonly byte[] _exampleContent = System.Text.Encoding.UTF8.GetBytes("Hello World!");
    private readonly IFileSystem _inputFileSystem = new MockFileSystem(null, Path.GetFullPath("/"));
    private readonly IFileSystem _extractFileSystem = new MockFileSystem(null, Path.GetFullPath("/"));

    private readonly string _inputDir = "/Input";

    public PhysicalSongProviderTests()
    {
        _inputFileSystem.Directory.CreateDirectory(_inputDir);
        _provider = new PhysicalSongProvider(_inputDir, _inputFileSystem);
    }

    [Fact]
    public void ShouldThrowOnCreateWithMissingDirectory()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            new PhysicalSongProvider("/DoesNotExist", _inputFileSystem));
    }

    [Fact]
    public void FileShouldExist()
    {
        _inputFileSystem.File.WriteAllBytes(Path.Combine(_inputDir, "example.txt"), _exampleContent);
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
        _inputFileSystem.File.WriteAllBytes(Path.Combine(_inputDir, "example.txt"), _exampleContent);

        using var entryStream = _provider.Open("example.txt");
        using var streamReader = new BinaryReader(entryStream);
        Assert.Equal(_exampleContent, streamReader.ReadBytes(_exampleContent.Length));
    }

    [Fact]
    public async Task ShouldExtractFiles()
    {
        _inputFileSystem.File.WriteAllBytes(Path.Combine(_inputDir, "example.txt"), _exampleContent);

        await _provider.CopyToAsync("/ExtractPath", _extractFileSystem);
        Assert.Equal(_exampleContent, 
            _extractFileSystem.File.ReadAllBytes("/ExtractPath/example.txt"));
    }

    [Fact]
    public async Task ShouldOverwriteFiles()
    {
        _inputFileSystem.File.WriteAllBytes(Path.Combine(_inputDir, "example.txt"), _exampleContent);
        _extractFileSystem.Directory.CreateDirectory("/ExtractPath");
        
        // We write a longer content than the overwriting content
        // This is to check that the file is deleted and recreated, instead of just the initial bytes being overwritten,
        // with corrupt file content left on the end
        _extractFileSystem.File.WriteAllBytes("/ExtractPath/example.txt",
            new byte[_exampleContent.Length + 10]);
        
        await _provider.CopyToAsync("/ExtractPath", _extractFileSystem);
        Assert.Equal(_exampleContent, _extractFileSystem.File.ReadAllBytes("/ExtractPath/example.txt"));
    }
}