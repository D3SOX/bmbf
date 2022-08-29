using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Backend.Models.BPList;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.Backend.Endpoints;

public class PlaylistsEndpoints : IEndpoints
{
    private readonly IPlaylistService _playlistService;

    public PlaylistsEndpoints(IPlaylistService playlistService)
    {
        _playlistService = playlistService;
    }

    [HttpGet("/playlists")]
    public async Task<HttpResponse> GetPlaylistsInfo()
    {
        var playlistsInfo = (await _playlistService.GetPlaylistsAsync())
            .Select(playlistPair => new PlaylistInfo(playlistPair.Value));

        return Responses.Json(playlistsInfo);
    }

    [HttpGet("/playlists/cover/{id}")]
    public async Task<HttpResponse> GetPlaylistCover(Request request)
    {
        if (!(await _playlistService.GetPlaylistsAsync()).TryGetValue(request.Param<string>("id"), out var matching)
            || matching.Image == null)
        {
            return Responses.NotFound();
        }

        return Responses.Stream(new MemoryStream(matching.Image), 200, "image/png");
    }

    [HttpPut("/playlists/cover/{id}")]
    public async Task<HttpResponse> PutPlaylistCover(Request request)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(request.Param<string>("id"), out var matching))
        {
            await using var tempStream = new MemoryStream();
            await request.Body.CopyToAsync(tempStream);
            matching.Image = tempStream.ToArray();
            return Responses.Ok();
        }

        return Responses.NotFound();
    }

    [HttpGet("/playlists/songs/{id}")]
    public async Task<HttpResponse> GetPlaylistSongs(Request request)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(request.Param<string>("id"), out var matching))
        {
            return Responses.Json(matching.Songs);
        }

        return Responses.NotFound();
    }

    [HttpPut("/playlists/songs/{id}")]
    public async Task<HttpResponse> PutPlaylistSongs(Request request)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(request.Param<string>("id"), out var matching))
        {
            matching.Songs = await request.JsonBody<List<BPSong>>();
            return Responses.Ok();
        }

        return Responses.NotFound();
    }

    [HttpPut("/playlists/update")]
    public async Task<HttpResponse> UpdatePlaylistInfo(Request request)
    {
        var newPlaylistInfo = await request.JsonBody<PlaylistInfo>();
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(newPlaylistInfo.Id, out var matching))
        {
            matching.SetPlaylistInfo(newPlaylistInfo);
            return Responses.Ok();
        }

        return Responses.NotFound();
    }


    [HttpDelete("/playlists/delete/{id}")]
    public async Task<HttpResponse> Delete(Request request)
    {
        if (await _playlistService.DeletePlaylistAsync(request.Param<string>("id")))
        {
            return Responses.Ok();
        }

        return Responses.NotFound(); // Playlist with given id was not in the cache
    }

    [HttpGet("/playlists/bplist/{id}")]
    // ReSharper disable once InconsistentNaming
    public async Task<HttpResponse> GetBPList(Request request)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(request.Param<string>("id"), out var matching))
        {
            // Return the full playlist, including the cover as base64
            // Usually we would transmit the cover and songs separately,
            // - since they are generally the largest part of the playlist
            // But sometimes people may want to download a BPList of a playlist they made in BMBF
            return Responses.Json(matching);
        }

        return Responses.NotFound();
    }

    [HttpPost("/playlists/add")]
    public async Task<HttpResponse> Add(Request request)
    {
        var playlistInfo = await request.JsonBody<PlaylistInfo>();
        var playlist = new Playlist(
            playlistInfo.PlaylistTitle,
            playlistInfo.PlaylistAuthor,
            playlistInfo.PlaylistDescription,
            new List<BPSong>()
        );
        await _playlistService.AddPlaylistAsync(playlist);
        return Responses.Json(playlist.Id);
    }

    [HttpPost("/playlists/save")]
    public async Task Save()
    {
        await _playlistService.SavePlaylistsAsync();
    }
}

