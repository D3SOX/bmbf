using Newtonsoft.Json;

namespace BMBF.Backend.Models;

/// <summary>
/// Represents the result of a mod install/uninstall
/// </summary>
public class ModActionResult
{
    public bool Success => Error == null;
    
    /// <summary>
    /// If non-null, specifies the error that occured during mod installation
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Error { get; set; }
}