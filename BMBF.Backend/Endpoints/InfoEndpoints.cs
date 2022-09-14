using System.Reflection;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using BMBF.Backend.Models;
using Hydra;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using Serilog;

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
        var nonLoopbackAddresses = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback && i.OperationalStatus == OperationalStatus.Up)
            .SelectMany(i => i.GetIPProperties().UnicastAddresses.Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork))
            .ToArray();

        if (nonLoopbackAddresses.Length == 0)
        {
            Log.Warning("Could not find any local IP address. Is the Quest connected to wifi?");
        }
        else if (nonLoopbackAddresses.Length > 1)
        {
            Log.Warning($"Multiple ({nonLoopbackAddresses.Length}) local IP addresses found. The first will be used");
        }

        return nonLoopbackAddresses.FirstOrDefault()?.Address.ToString();
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
