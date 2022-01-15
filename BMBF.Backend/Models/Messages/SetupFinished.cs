namespace BMBF.Backend.Models.Messages;

public class SetupFinished : IMessage
{
    public MessageType Type => MessageType.SetupFinished;
}