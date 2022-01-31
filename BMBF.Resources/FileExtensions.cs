using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BMBF.Resources
{
    public class FileExtensions
    {
        public Dictionary<string, string> CopyExtensions { get; set; }

        public List<string> PlaylistExtensions { get; set; }

        public List<string> ConfigExtensions { get; set; }

        [JsonConstructor]
        public FileExtensions(Dictionary<string, string> copyExtensions, List<string> playlistExtensions, List<string> configExtensions)
        {
            CopyExtensions = copyExtensions;
            PlaylistExtensions = playlistExtensions;
            ConfigExtensions = configExtensions;
        }
    }
}
