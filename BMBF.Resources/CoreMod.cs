using System;
using System.Text.Json.Serialization;
using Version = SemanticVersioning.Version;

namespace BMBF.Resources
{
    public class CoreMod
    {
        public string Id { get; set; }

        [JsonPropertyName("version")]
        public string VersionString
        {
            get => Version.ToString();
            set => Version = Version.Parse(value);
        }
        
        [JsonIgnore]
        public Version Version { get; set; }
        
        public Uri DownloadLink { get; set; }
        
        public string FileName { get; set; }
        
        [JsonConstructor]
        public CoreMod(string id, string versionString, Uri downloadLink, string fileName)
        {
            Id = id;
            Version = Version.Parse(versionString);
            DownloadLink = downloadLink;
            FileName = fileName;
        }
    }
}