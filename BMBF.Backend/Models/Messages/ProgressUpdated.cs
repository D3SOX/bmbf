using System;
using System.Text.Json.Serialization;

namespace BMBF.Backend.Models.Messages;

public class ProgressUpdated : IMessage
{
    public MessageType Type => MessageType.ProgressUpdated;

    public int Id { get; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Percentage { get; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ItemsCompleted { get; }
    
    public ProgressUpdated(IProgress progress, int id)
    {
        Id = id;
        if (progress is IChunkedProgress chunkedProgress)
        {
            ItemsCompleted = chunkedProgress.ItemsCompleted;
        }   else if (progress is IPercentageProgress percentageProgress)
        {
            Percentage = percentageProgress.Percentage;
        }
        else
        {
            throw new ArgumentException($"Received instance of {nameof(IProgress)} which could not be cast to" +
                                        $"{nameof(IPercentageProgress)} or {nameof(IChunkedProgress)}");
        }
    }
}
