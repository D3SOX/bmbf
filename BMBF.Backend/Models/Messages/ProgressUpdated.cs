namespace BMBF.Backend.Models.Messages;

public class ProgressUpdated : IMessage
{
    public MessageType Type => MessageType.ProgressUpdated;

    public int Id { get; }
    
    public int ItemsCompleted { get; }
    
    public ProgressUpdated(IProgress progress, int id)
    {
        Id = id;
        ItemsCompleted = progress.Completed;
    }
}
