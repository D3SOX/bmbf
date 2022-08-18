using System.Collections.Concurrent;

namespace BMBF.Backend.Models;

public class AuthConfig
{
    /// <summary>
    /// True if a username/password will be required to connect, false otherwise
    /// </summary>
    // TODO: Disable this when there is UI for auth
    public bool AuthEnabled { get; set; } = false;

    /// <summary>
    /// Username/password pairs which are accepted to access BMBF.
    /// Key is username, value is password
    /// </summary>
    public ConcurrentDictionary<string, string> Users { get; set; } = new();
}
