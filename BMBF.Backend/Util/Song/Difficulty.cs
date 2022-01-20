using System.Text.Json.Serialization;

namespace BMBF.Backend.Util.Song;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Expert = 3,
    ExpertPlus = 4
}