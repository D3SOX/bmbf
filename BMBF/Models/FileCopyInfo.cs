using Newtonsoft.Json;

namespace BMBF.Models
{
    /// <summary>
    /// Stores information about a particular file type that is copied to a directory on the quest
    /// </summary>
    public class FileCopyInfo
    {
        /// <summary>
        /// Destination that the files are copied to
        /// </summary>
        public string Destination { get; set; }
        
        /// <summary>
        /// Name of the mod registering this extension, if any
        /// </summary>
        public string? ModId { get; set; }
        
        [JsonConstructor]
        public FileCopyInfo(string destination, string? modId)
        {
            Destination = destination;
            ModId = modId;
        }
    }
}