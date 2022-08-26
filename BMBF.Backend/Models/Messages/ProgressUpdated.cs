namespace BMBF.Backend.Models.Messages;

public class ProgressUpdated : IMessage
{
    public MessageType Type => MessageType.ProgressUpdated;

    public long Id { get; }

    public int ItemsCompleted { get; }

    public ProgressUpdated(IProgress progress)
    {
        Id = progress.Id;
        ItemsCompleted = progress.Completed;
    }
}
