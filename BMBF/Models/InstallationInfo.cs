#nullable enable

using BMBF.Patching;
using Newtonsoft.Json;

namespace BMBF.Models
{
    /// <summary>
    /// Stores information about the current beat saber installation
    /// </summary>
    public class InstallationInfo
    {
        public string Version { get; set; }
        
        public int VersionCode { get; set; }
        
        public PatchManifest? ModTag { get; set; }
        
        [JsonConstructor]
        public InstallationInfo(string version, int versionCode, PatchManifest? modTag)
        {
            Version = version;
            VersionCode = versionCode;
            ModTag = modTag;
        }
    }
}