using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BMBF.Backend.Controllers;

[Route("[controller]")]
public class ImportController : Controller
{
    private readonly IFileImporter _fileImporter;

    public ImportController(IFileImporter fileImporter)
    {
        _fileImporter = fileImporter;
    }

    [HttpPost]
    public async Task<IActionResult> ImportFile()
    {
        if (!Request.Headers.TryGetValue("filename", out var fileName))
        {
            return BadRequest("Cannot import file without filename");
        }
            
        var result = await _fileImporter.TryImportAsync(Request.Body, fileName);
        if (result.Type == FileImportResultType.Failed)
        {
            Log.Error($"Failed to import file {fileName}: {result.Error}");
        }
        return Ok(result);
    }
}