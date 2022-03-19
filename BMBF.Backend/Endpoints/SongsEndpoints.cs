using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Services;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;
using MimeTypes;

namespace BMBF.Backend.Endpoints;

public class SongsEndpoints : IEndpoints
{
    private readonly ISongService _songService;
    private readonly IPlaylistService _playlistService;
    private readonly IFileSystem _io;

    public SongsEndpoints(ISongService songService, IFileSystem io, IPlaylistService playlistService)
    {
        _songService = songService;
        _io = io;
        _playlistService = playlistService;
    }

    [HttpGet("/songs")]
    public async Task<HttpResponse> Get()
    {
        return Responses.Json((await _songService.GetSongsAsync()).Values);
    }

    [HttpDelete("/songs/delete/{songHash}")]
    public async Task<HttpResponse> Delete(Request request)
    {
        if (await _songService.DeleteSongAsync(request.Param<string>("songHash")))
        {
            return Responses.Ok();
        }
        return Responses.NotFound(); // Song with given hash did not exist
    }

    [HttpGet("/songs/cover/{songHash}")]
    public async Task<HttpResponse> GetCover(Request request)
    {
        if (!(await _songService.GetSongsAsync()).TryGetValue(request.Param<string>("songHash"), out var matching))
        {
            return Responses.NotFound();
        }

        string fullCoverPath = Path.Combine(matching.Path, matching.CoverImageFileName);
        if (!_io.File.Exists(fullCoverPath))
        {
            return Responses.NotFound();
        }

        var coverStream = _io.File.OpenRead(fullCoverPath);
        if (!MimeTypeMap.TryGetMimeType(Path.GetExtension(matching.CoverImageFileName), out var mimeType))
        {
            return Responses.NotFound();
        }

        return Responses.Stream(coverStream, 200, mimeType);
    }

    [HttpPost("/songs/clean")]
    public async Task CleanUpSongs()
    {
        // Keep any songs in a playlist
        var hashesToKeep = (await _playlistService.GetPlaylistsAsync())
            .Values
            .SelectMany(p => p.Songs.Select(bpSong => bpSong.Hash))
            .ToHashSet();

        // Delete all other songs
        foreach (var song in (await _songService.GetSongsAsync())
                 .Values
                 .Where(s => !hashesToKeep.Contains(s.Hash)))
        {
            await _songService.DeleteSongAsync(song.Hash);
        }
    }
}
