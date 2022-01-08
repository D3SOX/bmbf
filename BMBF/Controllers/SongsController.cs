using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BMBF.Models;
using BMBF.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMBF.Controllers
{
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

        [HttpDelete]
        [Route("[action]/{songHash}")]
        public async Task<IActionResult> Delete(string songHash)
        {
            if (await _songService.DeleteSongAsync(songHash))
            {
                return Ok();
            }
            return NotFound(); // Song with given hash did not exist
        }

        [HttpGet]
        [Route("cover/{songHash}")]
        public async Task GetCover(string songHash)
        {
            if((await _songService.GetSongsAsync()).TryGetValue(songHash, out var matching))
            {
                HttpContext.Response.StatusCode = (int) HttpStatusCode.OK;
                HttpContext.Response.ContentType = MimeTypes.MimeTypeMap.GetMimeType(matching.CoverImageFileName);
                string fullCoverPath = Path.Combine(matching.Path, matching.CoverImageFileName);
                if (System.IO.File.Exists(fullCoverPath))
                {
                    await using var coverStream =
                        System.IO.File.OpenRead(fullCoverPath);

                    await coverStream.CopyToAsync(HttpContext.Response.Body);
                    return;
                }
            }

            HttpContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
        }
    }
}