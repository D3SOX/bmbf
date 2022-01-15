using BMBF.Backend.Models.Setup;

namespace BMBF.Backend.Models.Messages;

public class SetupStatusUpdate : IMessage
{
    public SetupStatusUpdate(SetupStatus status)
    {
        Status = status;
    }

    public MessageType Type => MessageType.SetupStatusUpdate;

    public SetupStatus Status { get; set; }
}