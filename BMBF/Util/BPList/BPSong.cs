using Newtonsoft.Json;

namespace BMBF.Util.BPList
{
    // ReSharper disable once InconsistentNaming
    public class BPSong
    {
        public string Hash { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? SongName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Key { get; set; }

        [JsonConstructor]
        public BPSong(string hash, string? songName, string? key)
        {
            Hash = hash;
            SongName = songName;
            Key = key;
        }
    }
}