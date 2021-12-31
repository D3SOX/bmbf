using Newtonsoft.Json;

namespace BMBF.Resources
{
    /// <summary>
    /// Stores the information about a particular binary diff
    /// </summary>
    public class DiffInfo
    {
        /// <summary>
        /// Version that the diff downgrades from
        /// </summary>
        public string FromVersion { get; set; }

        /// <summary>
        /// Version that the diff downgrades to
        /// </summary>
        public string ToVersion { get; set; }

        /// <summary>
        /// Name of the diff within the diffs directory
        /// </summary>
        public string? Name { get; set; }
        
        // TODO: Source and destination hash for sanity checking

        [JsonConstructor]
        public DiffInfo(string fromVersion, string toVersion, string? name)
        {
            FromVersion = fromVersion;
            ToVersion = toVersion;
            Name = name;
        }
    }
}