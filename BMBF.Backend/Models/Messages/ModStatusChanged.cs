namespace BMBF.Backend.Models.Messages;

public class ModStatusChanged : IMessage
{
    public ModStatusChanged(string id, bool newStatus)
    {
        Id = id;
        NewStatus = newStatus;
    }

    public MessageType Type => MessageType.ModStatusChanged;

    public string Id { get; set; }

    public bool NewStatus { get; set; }
}
