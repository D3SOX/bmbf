using System;
using Newtonsoft.Json;

namespace BMBF.Resources
{
    /// <summary>
    /// Stored in the resources repository to indicate which modloader version to use
    /// </summary>
    public class ModLoaderVersion
    { 
        public string Version { get; set; }
        
#region Download Links
        public Uri ModLoader32 { get; set; }
        
        public Uri Main32 { get; set; }
        
        public Uri ModLoader64 { get; set; }
        
        public Uri Main64 { get; set; }
        
#endregion
        
        [JsonConstructor]
        public ModLoaderVersion(string version, Uri modLoader32, Uri main32, Uri modLoader64, Uri main64)
        {
            Version = version;
            ModLoader32 = modLoader32;
            Main32 = main32;
            ModLoader64 = modLoader64;
            Main64 = main64;
        }
    }
}