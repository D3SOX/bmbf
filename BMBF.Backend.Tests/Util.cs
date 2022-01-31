using System.Collections.Immutable;
using System.IO.Abstractions;
using BMBF.Backend.Models;
using BMBF.Backend.Models.BPList;
using BMBF.Backend.Util;

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
}