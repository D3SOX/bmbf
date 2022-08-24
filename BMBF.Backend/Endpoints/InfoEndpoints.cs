using System.Reflection;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using BMBF.Backend.Models;
using Hydra;
using System.Net;
using System.Linq;
using System.Net.Sockets;

namespace BMBF.Backend.Endpoints;

public class InfoEndpoints : IEndpoints
{
    private readonly string? _version;

    public InfoEndpoints()
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (assemblyVersion != null)
        {
            _version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
        }
    }

    private string? GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
    }

    [HttpGet("/host")]
    public HttpResponse GetHost(Request request)
    {
        if (_version == null)
        {
            return Responses.NotFound("Could not determine BMBF version");
        }

        var localIp = GetLocalIpAddress();
        if (localIp == null)
        {
            return Responses.NotFound("Could not find local IP address");
        }

        var remoteIp = (request.Inner.Remote as IPEndPoint)?.Address.ToString();
        if (remoteIp == null)
        {
            return Responses.NotFound("Could not determine connecting IP address");
        }

        return Responses.Json(new HostInfo(localIp, remoteIp, _version));
    }
}
