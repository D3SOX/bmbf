// Authenticating loopback clients can be helpful for testing purposes
//#define AUTHENTICATE_LOOPBACK

using System;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.WebServer;
using Hydra;
using Serilog;

namespace BMBF.Backend.Implementations;

public class AuthService : IAuthService
{
    private AuthConfig? _authConfig;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly string _authConfigPath;
    private readonly IFileSystem _io;
    private readonly SemaphoreSlim _authLock = new(1);

    public AuthService(JsonSerializerOptions serializerOptions,
        BMBFSettings settings,
        IFileSystem io)
    {
        _serializerOptions = serializerOptions;
        _io = io;
        _authConfigPath = Path.Combine(settings.RootDataPath, settings.AuthFileName);
    }
    
    /// <summary>
    /// Default unauthorized response from BMBF.
    /// </summary>
    private HttpResponse Unauthorized => new(401)
    {
        Headers =
        {
            ["WWW-Authenticate"] = "Basic realm=\"BMBF\""
        }
    };

    public async Task<HttpResponse> Authenticate(Request request, Handler next)
    {
        var cfg= await GetAuthConfig();
        if (!cfg.AuthEnabled)
        {
            // Skip straight to the next handler if authentication is disabled
            return await next(request);
        }
        
#if AUTHENTICATE_LOOPBACK
#else 
        // If the peer is a loopback address, we skip authentication since BMBF is being viewed inside the Quest
        if(request.Inner.Remote is IPEndPoint endPoint && IPAddress.IsLoopback(endPoint.Address))
        {
            return await next(request);
        }
#endif

        if (cfg.Users.Count == 0)
        {
            return Responses.Text("This instance of BMBF is currently only accessible from inside the Quest.\n" +
                                  "Please add a user account in the BMBF app and refresh the page," +
                                  " then login with the credentials.", 403);
        }

        // If no authorization provided, return unauthorized
        if (!request.Headers.TryGetValue("Authorization", out var auth))
        {
            return Unauthorized;
        }

        // Check that the authorization provided is Basic HTTP credentials
        string authString = auth.ToString();
        if (!authString.StartsWith("Basic "))
        {
            return Unauthorized;
        }

        string authSection = authString[6..];
        string decodedUserPassPair;
        try
        {
            // Convert the authentication from base64 to a username:password format
            decodedUserPassPair = Encoding.UTF8.GetString(Convert.FromBase64String(authSection));
        }
        catch (FormatException)
        {
            return Unauthorized;
        }
        
        string[] split = decodedUserPassPair.Split(":");
        if (split.Length != 2)
        {
            return Unauthorized;
        }

        string username = split[0];
        string password = split[1];
        // Make sure that a user exists with the given username, and that its password matches the given password
        if (!cfg.Users.TryGetValue(username, out string? expectedPassword) || password != expectedPassword)
        {
            return Unauthorized;
        }

        return await next(request);
    }

    public async ValueTask<AuthConfig> GetAuthConfig()
    {
        if (_authConfig != null)
        {
            return _authConfig;
        }

        try
        {
            await _authLock.WaitAsync();
            if (_authConfig != null)
            {
                return _authConfig;
            }
            
            await using var authFileStream = _io.File.OpenRead(_authConfigPath);
            _authConfig = await JsonSerializer.DeserializeAsync<AuthConfig>(authFileStream, _serializerOptions)
                          ?? throw new NullReferenceException("Deserialized result was null");
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException)
            {
                Log.Debug("Auth config not found, generating new");
            }
            else
            {
                Log.Error(ex, $"Failed to load auth config from {_authConfigPath}, generating new config");
            }

            _authConfig = new AuthConfig();
            await SaveAuthConfig();
        }
        finally
        {
            _authLock.Release();
        }
        return _authConfig;
    }

    public async Task SaveAuthConfig()
    {
        await _authLock.WaitAsync();
        try
        {
            if (_io.File.Exists(_authConfigPath))
            {
                _io.File.Delete(_authConfigPath);
            }
        
            await using var authFileStream = _io.File.OpenWrite(_authConfigPath);
            await JsonSerializer.SerializeAsync(authFileStream, _authConfig, _serializerOptions);
        }
        finally
        {
            _authLock.Release();
        }
    }
}
