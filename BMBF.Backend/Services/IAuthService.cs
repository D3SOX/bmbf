using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.WebServer;
using Hydra;

namespace BMBF.Backend.Services;

public interface IAuthService
{
    /// <summary>
    /// Attempts to authenticate the given request.
    /// </summary>
    /// <param name="request">Request to authenticate</param>
    /// <param name="next">The next handler in the middleware pipeline</param>
    /// <returns>e.g. 401 if no authorization provided, or the response from <paramref name="next"/></returns>
    Task<HttpResponse> Authenticate(Request request, Handler next);

    /// <summary>
    /// Gets the authentication config
    /// </summary>
    /// <returns>The authentication config</returns>
    ValueTask<AuthConfig> GetAuthConfig();

    /// <summary>
    /// Saves the authentication config
    /// </summary>
    Task SaveAuthConfig();

}
