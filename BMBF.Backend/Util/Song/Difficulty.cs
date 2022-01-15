using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BMBF.Backend.Util.Song;

[JsonConverter(typeof(StringEnumConverter))]
public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Expert = 3,
    ExpertPlus = 4
}