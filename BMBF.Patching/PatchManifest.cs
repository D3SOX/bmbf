using System.Collections.Generic;
using System.Text.Json.Serialization;
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
        [JsonIgnore]
        public Version? PatcherVersion { get; set; }

        /// <summary>
        /// String representation of <see cref="PatcherVersion"/>
        /// </summary>
        [JsonPropertyName("patcherVersion")]
        public string? PatcherVersionString
        {
            get => PatcherVersion?.ToString();
            set => PatcherVersion = value == null ? null : Version.Parse(value);
        }

        /// <summary>
        /// Name of the modloader that the APK was patched with
        /// </summary>
        public string? ModloaderName { get; set; }

        /// <summary>
        /// Version of the modloader that the APK was patched with
        /// </summary>
        [JsonIgnore]
        public Version? ModloaderVersion { get; set; }

        /// <summary>
        /// String representation of <see cref="ModloaderVersion"/>
        /// </summary>
        [JsonPropertyName("modloaderVersion")]
        public string? ModloaderVersionString
        {
            get => ModloaderVersion?.ToString();
            set => ModloaderVersion = value == null ? null : Version.Parse(value);
        }

        /// <summary>
        /// A list of all the modified files in the APK
        /// </summary>
        public HashSet<string> ModifiedFiles { get; set; } = new();

        [JsonConstructor]
        public PatchManifest(string patcherName, string? patcherVersionString)
        {
            PatcherName = patcherName;
            PatcherVersionString = PatcherVersionString;
        }
    }
}