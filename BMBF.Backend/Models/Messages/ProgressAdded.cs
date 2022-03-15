
using System.Text.Json.Serialization;

namespace BMBF.Backend.Models.Messages;

public class ProgressAdded : IMessage
{
    public ProgressAdded(IProgress progress, int id)
    {
        Name = progress.Name;
        Id = id;
        if (progress is IChunkedProgress chunkedProgress)
        {
            TotalItems = chunkedProgress.TotalItems;
        }
    }

    public string Name { get; }
    
    public int Id { get; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalItems { get; }
    public MessageType Type => MessageType.ProgressAdded;
}
