namespace BMBF.Backend.Models.Messages;

public class ModRemoved : IMessage
{
    public ModRemoved(string id)
    {
        Id = id;
    }

    public MessageType Type => MessageType.ModRemoved;

    public string Id { get; }
}
