using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using MimeTypes;

namespace BMBF.Backend.Controllers;

[Route("[controller]")]
public class SongsController : Controller
{
    private readonly ISongService _songService;

    public SongsController(ISongService songService)
    {
        _songService = songService;
    }

    [HttpGet]
    public async Task<IEnumerable<Song>> Get()
    {
        return (await _songService.GetSongsAsync()).Values;
    }

    [HttpDelete("[action]/{songHash}")]
    public async Task<IActionResult> Delete(string songHash)
    {
        if (await _songService.DeleteSongAsync(songHash))
        {
            return Ok();
        }
        return NotFound(); // Song with given hash did not exist
    }

    [HttpGet("cover/{songHash}")]
    public async Task<IActionResult> GetCover(string songHash)
    {
        if (!(await _songService.GetSongsAsync()).TryGetValue(songHash, out var matching))
        {
            return NotFound();
        }

        string fullCoverPath = Path.Combine(matching.Path, matching.CoverImageFileName);
        if (!System.IO.File.Exists(fullCoverPath))
        {
            return NotFound();
        }

        var coverStream = System.IO.File.OpenRead(fullCoverPath);
        if (!MimeTypeMap.TryGetMimeType(Path.GetExtension(matching.CoverImageFileName), out var mimeType))
        {
            return NotFound();
        }

        return File(coverStream, mimeType);
    }
}