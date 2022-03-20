namespace BMBF.Backend.Models.Messages;

public class ProgressRemoved : IMessage
{
    public MessageType Type => MessageType.ProgressRemoved;

    public long Id { get; }
    
    public ProgressRemoved(IProgress progress)
    {
        Id = progress.Id;
    }
}
