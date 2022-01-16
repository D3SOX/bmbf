﻿using System.Threading.Tasks;
using BMBF.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace BMBF.Backend.Controllers;

[Route("[controller]")]
public class BeatSaberController : Controller
{
    private readonly IBeatSaberService _beatSaberService;

    public BeatSaberController(IBeatSaberService beatSaberService)
    {
        _beatSaberService = beatSaberService;
    }

    [HttpGet("install")]
    public async Task<IActionResult> GetInstallInfo()
    {
        var installInfo = await _beatSaberService.GetInstallationInfoAsync();
        if (installInfo is null)
        {
            return NotFound("Beat Saber is not installed");
        }
        return new OkObjectResult(installInfo);
    }
}