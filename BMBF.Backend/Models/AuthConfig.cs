using System.Collections.Concurrent;

namespace BMBF.Backend.Models;

public class AuthConfig
{
    /// <summary>
    /// True if a username/password will be required to connect, false otherwise
    /// </summary>
    public bool AuthEnabled { get; set; } = true;

    /// <summary>
    /// Username/password pairs which are accepted to access BMBF.
    /// Key is username, value is password
    /// </summary>
    public ConcurrentDictionary<string, string> Users { get; set; } = new();
}
