using System.Collections.Generic;
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
    }
}