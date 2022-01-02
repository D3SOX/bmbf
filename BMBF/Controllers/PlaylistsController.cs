﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BMBF.Models;
using BMBF.Services;
using BMBF.Util.BPList;
using Microsoft.AspNetCore.Mvc;

namespace BMBF.Controllers
{
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
            return (await _playlistService.GetPlaylistsAsync()).Select(playlistPair => new PlaylistInfo(playlistPair.Value)
            {
                PlaylistId = playlistPair.Key
            });
        }

        [HttpGet]
        [Route("cover/{playlistId}")]
        public async Task GetPlaylistCover(string playlistId)
        {
            if((await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching))
            {
                HttpContext.Response.StatusCode = (int) HttpStatusCode.OK;
                HttpContext.Response.ContentType = "image/png";
                await HttpContext.Response.Body.WriteAsync(matching.Image);
                return;
            }

            HttpContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
        }

        [HttpPut]
        [Route("cover/{playlistId}")]
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

        [HttpGet]
        [Route("songs/{playlistId}")]
        public async Task<IActionResult> GetPlaylistSongs(string playlistId)
        {
            if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching))
            {
                return Ok(matching.Songs);
            }
            return NotFound();
        }

        [HttpPut]
        [Route("songs/{playlistId}")]
        public async Task<IActionResult> PutPlaylistSongs(string playlistId, [FromBody] ObservableCollection<BPSong> songs)
        {
            if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var matching))
            {
                matching.Songs = songs;
                return Ok();
            }
            return NotFound();
        }

        [HttpPut]
        [Route("update/{playlistId}")]
        public async Task<IActionResult> UpdatePlaylistInfo([FromBody] PlaylistInfo newPlaylistInfo)
        {
            if (newPlaylistInfo.PlaylistId == null)
            {
                return BadRequest("Must specify playlist ID");
            }
            
            if ((await _playlistService.GetPlaylistsAsync()).TryGetValue(newPlaylistInfo.PlaylistId, out var matching))
            {
                matching.PlaylistTitle = newPlaylistInfo.PlaylistTitle;
                matching.PlaylistAuthor = newPlaylistInfo.PlaylistAuthor;
                matching.PlaylistDescription = newPlaylistInfo.PlaylistDescription;
                // ID is deliberately not reflected here - playlist ID is managed by the backend
                return Ok();
            }
            return NotFound();
        }
        
        
        [HttpDelete]
        [Route("[action]")]
        public async Task<IActionResult> Delete([FromBody] string playlistId)
        {
            if (await _playlistService.DeletePlaylistAsync(playlistId))
            {
                return Ok();
            }
            return BadRequest(); // Playlist with given path was not in the cache
        }

        [HttpGet]
        [Route("bplist/{playlistId}")]
        // ReSharper disable once InconsistentNaming
        public async Task<IActionResult> GetBPList(string playlistId)
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

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Add([FromBody] PlaylistInfo playlistInfo)
        {
            Playlist playlist = new Playlist(
                playlistInfo.PlaylistTitle,
                playlistInfo.PlaylistAuthor,
                playlistInfo.PlaylistDescription,
                new ObservableCollection<BPSong>(),
                null
            );
            await _playlistService.AddPlaylistAsync(playlist);
            return Ok();
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Save()
        {
            await _playlistService.SavePlaylistsAsync();
            return Ok();
        }
    }
}