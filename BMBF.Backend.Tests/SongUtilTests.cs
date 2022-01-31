using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BMBF.Backend.Util;
using BMBF.Backend.Util.Song;
using Moq;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BMBF.Backend.Tests;

public class SongUtilTests
{
    [Fact]
    public async Task ShouldReturnNullIfNoInfoDat()
    {
        // All methods will be stubbed, so no files will exist
        var folderProvider = Mock.Of<ISongProvider>();
        Assert.Null(await SongUtil.TryLoadSongInfoAsync(folderProvider));
    }

    [Theory]
    [InlineData("info.dat")]
    [InlineData("Info.dat")]
    public async Task ShouldAllowBothInfoDatPaths(string infoDatPath)
    {
        var folderProvider = new Mock<ISongProvider>();
        folderProvider.Setup(f => f.Exists(infoDatPath)).Returns(() => true);
        var exampleInfoDat = new BeatmapInfoDat
        {
            DifficultyBeatmapSets = new List<DifficultyBeatmapSet>() // Avoid song import failing due to null difficulty set list
        };

        folderProvider.Setup(f => f.Open(infoDatPath)).Returns(() =>
        {
            var infoStream = new MemoryStream();
            JsonSerializer.Serialize(infoStream, exampleInfoDat);
            infoStream.Position = 0;
            return infoStream;
        });

        Assert.NotNull(await SongUtil.TryLoadSongInfoAsync(folderProvider.Object));
    }

    [Fact]
    public async Task ShouldMatchSongDetails()
    {
        var folderProvider = new Mock<ISongProvider>();
        folderProvider.Setup(f => f.Exists("info.dat")).Returns(() => true);
        var exampleInfoDat = new BeatmapInfoDat
        {
            DifficultyBeatmapSets = new List<DifficultyBeatmapSet>(),
            SongName = "Example song name",
            SongSubName = "Example subname",
            SongAuthorName = "Example artist",
            LevelAuthorName = "Unicorns",
            CoverImageFilename = "cover.jpg"
        };

        folderProvider.Setup(f => f.Open("info.dat")).Returns(() =>
        {
            var infoStream = new MemoryStream();
            JsonSerializer.Serialize(infoStream, exampleInfoDat);
            infoStream.Position = 0;
            return infoStream;
        });

        var song = await SongUtil.TryLoadSongInfoAsync(folderProvider.Object);
        Debug.Assert(song != null);

        // Make sure that all of the song details match
        Assert.Equal(exampleInfoDat.SongName, song.SongName);
        Assert.Equal(exampleInfoDat.SongSubName, song.SongSubName);
        Assert.Equal(exampleInfoDat.SongAuthorName, song.SongAuthorName);
        Assert.Equal(exampleInfoDat.LevelAuthorName, song.LevelAuthorName);
        Assert.Equal(exampleInfoDat.CoverImageFilename, song.CoverImageFileName);
    }

    [Fact]
    public async Task ShouldHaveCorrectHash()
    {
        var song = await SongUtil.TryLoadSongInfoAsync(Util.ExampleSongProvider);
        Debug.Assert(song != null);

        // ff9
        Assert.Equal("CB9F1581FF6C09130C991DB8823C5953C660688F", song.Hash);
    }
}