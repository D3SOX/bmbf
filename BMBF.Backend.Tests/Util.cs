using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using BMBF.Backend.Models;
using BMBF.Backend.Models.BPList;
using BMBF.Backend.Util;
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
        ImmutableList<BPSong>.Empty,
        null
    );

    public static byte[] ExampleFileContent { get; } = System.Text.Encoding.UTF8.GetBytes("Hello World!");

    /// <summary>
    /// Creates a stream containing <see cref="ExampleFileContent"/>
    /// </summary>
    /// <returns>Stream containing <see cref="ExampleFileContent"/>, seeked to position 0</returns>
    public static Stream CreateExampleContentStream()
    {
        var stream = new MemoryStream();
        stream.Write(ExampleFileContent);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Asserts that the given stream contains <see cref="ExampleFileContent"/>
    /// </summary>
    /// <param name="stream">Stream to check</param>
    public static void AssertIsExampleContent(Stream stream)
    {
        using var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        
        Assert.Equal(ExampleFileContent, memStream.ToArray());
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
}
