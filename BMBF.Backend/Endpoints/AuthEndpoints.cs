using System.Collections.Generic;
using System.Threading.Tasks;
using BMBF.Backend.Services;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.Backend.Endpoints;

/// <summary>
/// Endpoints used to setup/change/remove usernames/passwords for authentication.
/// NOTE: These endpoints should ONLY EVER be accessible by loopback.
/// Allowing non-loopback clients to access these would allow anybody to create arbitrary users
/// </summary>
public class AuthEndpoints
{
    private readonly IAuthService _authService;

    public AuthEndpoints(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("users")]
    public async Task<HttpResponse> GetUsers()
    {
        return Responses.Json((await _authService.GetAuthConfig()).Users.Keys);
    }

    [HttpDelete("users")]
    public async Task DeleteUsers(Request req)
    {
        var cfg = await _authService.GetAuthConfig();
        await foreach (string toRemove in req.JsonBody<IAsyncEnumerable<string>>())
        {
            cfg.Users.TryRemove(toRemove, out _);
        }
        await _authService.SaveAuthConfig();
    }

    [HttpPost("users")]
    public async Task AddUsers(Request req)
    {
        var cfg = await _authService.GetAuthConfig();
        foreach (var userPair in req.JsonBody<Dictionary<string, string>>())
        {
            cfg.Users[userPair.Key] = userPair.Value;
        }
        await _authService.SaveAuthConfig();
    }

    [HttpGet("enabled")]
    public async Task<HttpResponse> GetEnabled() => Responses.Json((await _authService.GetAuthConfig()).AuthEnabled);

    [HttpPost("enabled")]
    public async Task SetEnabled(Request req)
    {
        (await _authService.GetAuthConfig()).AuthEnabled = req.JsonBody<bool>();
        await _authService.SaveAuthConfig();
    }
}
