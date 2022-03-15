namespace BMBF.Backend.Models.Messages;

public class ProgressRemoved : IMessage
{
    public MessageType Type => MessageType.ProgressRemoved;

    public int Id { get; }
    
    public ProgressRemoved(int id)
    {
        Id = id;
    }
}
