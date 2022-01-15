using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMBF.Models;

namespace BMBF.Services;

/// <summary>
/// Manages copy extensions
/// </summary>
public interface IExtensionsService
{
    /// <summary>
    /// The current copy extensions. Key is file extension without a period prefix.
    /// </summary>
    ConcurrentDictionary<string, FileCopyInfo> CopyExtensions { get; }
        
    /// <summary>
    /// File extensions (without period prefix) of playlist files
    /// </summary>
    HashSet<string> PlaylistExtensions { get; }
        
    /// <summary>
    /// File extensions (without period prefix) of config files
    /// </summary>
    HashSet<string> ConfigExtensions { get; }
        
    /// <summary>
    /// Loads the default extensions from BMBF resources if not already loaded.
    /// If no internet is available, the built-in extensions will be used.
    /// </summary>
    ValueTask LoadExtensions();
}