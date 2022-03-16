using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using BMBF.Backend.Models;
using BMBF.Backend.Models.BPList;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using Moq;
using Xunit;

namespace BMBF.Backend.Tests;

public static class Util
{
    public static ISongProvider ExampleSongProvider { get; } = new PhysicalSongProvider(
        "./Resources/ExampleSong",
        new FileSystem()
    );

    public static Playlist ExamplePlaylist => new(
        "Example Playlist",
        "Unicorns",
        "Example",
        ImmutableList<BPSong>.Empty
    );

    public static byte[] ExampleFileContent { get; } = System.Text.Encoding.UTF8.GetBytes("Hello World!");

    /// <summary>
    /// Creates a <see cref="MemoryStream"/> containing <see cref="ExampleFileContent"/>
    /// </summary>
    /// <returns>Stream containing <see cref="ExampleFileContent"/>, seeked to position 0</returns>
    public static MemoryStream CreateExampleContentStream() => CopyToMemoryStream(ExampleFileContent);

    /// <summary>
    /// Copies the given content to a <see cref="MemoryStream"/>, seeks it to position 0 and returns it.
    /// </summary>
    /// <param name="content"></param>
    /// <returns>Stream containing <paramref name="content"/>, seeked to position 0</returns>
    public static MemoryStream CopyToMemoryStream(byte[] content)
    {
        var stream = new MemoryStream();
        stream.Write(content);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Asserts that the given stream contains <see cref="ExampleFileContent"/> (and nothing else)
    /// </summary>
    /// <param name="stream">Stream to check</param>
    public static void AssertIsExampleContent(Stream stream) => AssertStreamContainsContent(stream, ExampleFileContent);

    /// <summary>
    /// Asserts that <see cref="stream"/> contains <see cref="content"/> (and nothing else)
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="content"></param>
    public static void AssertStreamContainsContent(Stream stream, byte[] content)
    {
        Assert.True(IsStreamContentEqual(stream, content));
    }

    /// <summary>
    /// Finds if <paramref name="stream"/> contains <paramref name="content"/>
    /// </summary>
    /// <param name="stream">Stream to check</param>
    /// <param name="content">Content to compare to that of the stream</param>
    /// <returns>True if (and only if) <paramref name="stream"/> contains <paramref name="content"/>
    /// (and nothing else)</returns>
    public static bool IsStreamContentEqual(Stream stream, byte[] content)
    {
        using var memStream = new MemoryStream();
        stream.CopyTo(memStream);

        return memStream.ToArray().SequenceEqual(content);
    }

    /// <summary>
    /// Creates a reliable mock <see cref="IFileSystem"/>.
    /// This fixes any issues with / being interpreted as C:/ or D:/ depending on the location of the test runner
    /// buy setting the root path of the file system to the full path of / (<see cref="Path.GetFullPath(string)"/>)
    /// </summary>
    /// <returns>Mock <see cref="IFileSystem"/></returns>
    public static MockFileSystem CreateMockFileSystem() => new(
        new Dictionary<string, MockFileData>(),
        Path.GetFullPath("/"));

    public static IProgressService CreateMockProgressService()
    {
        var mock = new Mock<IProgressService>();
        mock.Setup(p => p.CreateProgress(It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<int>()))
            .Returns(Mock.Of<IProgress>());
            

        return mock.Object;
    }
}
