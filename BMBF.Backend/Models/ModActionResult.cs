using System.Text.Json.Serialization;

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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}