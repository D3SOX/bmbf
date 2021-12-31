using System.Collections.Generic;
using Newtonsoft.Json;
using SemanticVersioning;

namespace BMBF.Patching
{
    /// <summary>
    /// Stored in the APK to show BMBF how it has been patched
    /// TODO: Store manifest differences
    /// </summary>
    public class PatchManifest
    {
        /// <summary>
        /// Name of the program used to patch the APK
        /// </summary>
        public string PatcherName { get; set; }
        
        /// <summary>
        /// Version of the program used to patch the APK. Null if unknown
        /// </summary>
        public Version? PatcherVersion { get; set; }
        
        /// <summary>
        /// Name of the modloader that the APK was patched with
        /// </summary>
        public string? ModloaderName { get; set; }
        
        /// <summary>
        /// Version of the modloader that the APK was patched with
        /// </summary>
        public Version? ModloaderVersion { get; set; }

        /// <summary>
        /// A list of all the modified files in the APK
        /// </summary>
        public HashSet<string> ModifiedFiles { get; set; } = new HashSet<string>();
        
        [JsonConstructor]
        public PatchManifest(string patcherName, Version? patcherVersion)
        {
            PatcherName = patcherName;
            PatcherVersion = patcherVersion;
        }
    }
}