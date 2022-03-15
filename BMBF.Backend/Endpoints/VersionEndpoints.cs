using System.Reflection;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.Backend.Endpoints;

public class VersionEndpoints : IEndpoints
{
    private readonly string? _version;

    public VersionEndpoints()
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (assemblyVersion != null)
        {
            _version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
        }
    }
    
    [HttpGet("/version")]
    public HttpResponse Version()
    {
        if (_version == null)
        {
            return Responses.NotFound("Could not determine BMBF version");
        }
        return Responses.Text(_version);
    }
}
