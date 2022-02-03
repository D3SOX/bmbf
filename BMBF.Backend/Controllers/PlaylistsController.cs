using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Backend.Models.BPList;
using Microsoft.AspNetCore.Mvc;

namespace BMBF.Backend.Controllers;

[Route("[controller]")]
public class PlaylistsController : Controller
{
    private readonly IPlaylistService _playlistService;

    public PlaylistsController(IPlaylistService playlistService)
    {
        _playlistService = playlistService;
    }

    [HttpGet]
    public async Task<IEnumerable<PlaylistInfo>> GetPlaylistsInfo()
    {
        return (await _playlistService.GetPlaylistsAsync()).Select(playlistPair => new PlaylistInfo(playlistPair.Value));
    }

    [HttpGet("cover/{playlistId}")]
    [Produces("image/png")]
    public async Task<IActionResult> GetPlaylistCover(string playlistId)
    {
        if (!(await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching) || matching.Image == null)
        {
            return NotFound();
        }

        return File(new MemoryStream(matching.Image), "image/png");
    }

    [HttpPut("cover/{playlistId}")]
    public async Task<IActionResult> PutPlaylistCover(string playlistId)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching))
        {
            await using var tempStream = new MemoryStream();
            await Request.Body.CopyToAsync(tempStream);
            matching.Image = tempStream.ToArray();
            return Ok();
        }
        return NotFound();
    }

    [HttpGet("songs/{playlistId}")]
    public async Task<ActionResult<ImmutableList<BPSong>>> GetPlaylistSongs(string playlistId)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching))
        {
            return Ok(matching.Songs);
        }
        return NotFound();
    }

    [HttpPut("songs/{playlistId}")]
    public async Task<IActionResult> PutPlaylistSongs(string playlistId, [FromBody] ImmutableList<BPSong> songs)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching))
        {
            matching.Songs = songs;
            return Ok();
        }
        return NotFound();
    }

    [HttpPut("update/{playlistId}")]
    public async Task<IActionResult> UpdatePlaylistInfo([FromBody] PlaylistInfo newPlaylistInfo)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(newPlaylistInfo.Id, out var matching))
        {
            matching.SetPlaylistInfo(newPlaylistInfo);
            return Ok();
        }
        return NotFound();
    }


    [HttpDelete("[action]/{playlistId}")]
    public async Task<IActionResult> Delete(string playlistId)
    {
        if (await _playlistService.DeletePlaylistAsync(playlistId))
        {
            return Ok();
        }
        return NotFound(); // Playlist with given id was not in the cache
    }

    [HttpGet("bplist/{playlistId}")]
    // ReSharper disable once InconsistentNaming
    public async Task<ActionResult<Playlist>> GetBPList(string playlistId)
    {
        if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching))
        {
            // Return the full playlist, including the cover as base64
            // Usually we would transmit the cover and songs separately,
            // - since they are generally the largest part of the playlist
            // But sometimes people may want to download a BPList of a playlist they made in BMBF
            return Ok(matching);
        }
        return NotFound();
    }

    [HttpPost("[action]")]
    public async Task<ActionResult<string>> Add([FromBody] PlaylistInfo playlistInfo)
    {
        Playlist playlist = new Playlist(
            playlistInfo.PlaylistTitle,
            playlistInfo.PlaylistAuthor,
            playlistInfo.PlaylistDescription,
            ImmutableList.Create<BPSong>()
        );
        await _playlistService.AddPlaylistAsync(playlist);
        return Ok(playlist.Id);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Save()
    {
        await _playlistService.SavePlaylistsAsync();
        return Ok();
    }
}
