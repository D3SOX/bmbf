using System.Text.Json.Serialization;
using BMBF.Patching;

namespace BMBF.Backend.Models;

/// <summary>
/// Stores information about the current beat saber installation
/// </summary>
public class InstallationInfo
{
    public string Version { get; set; }
        
    public int VersionCode { get; set; }
        
    public PatchManifest? ModTag { get; set; }
        
    [JsonIgnore]
    public string ApkPath { get; set; }
        
    public InstallationInfo(string version, int versionCode, PatchManifest? modTag, string apkPath)
    {
        Version = version;
        VersionCode = versionCode;
        ModTag = modTag;
        ApkPath = apkPath;
    }
}