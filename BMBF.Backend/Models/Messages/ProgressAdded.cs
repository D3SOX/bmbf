namespace BMBF.Backend.Models.Messages;

public class ProgressAdded : IMessage
{
    public ProgressAdded(IProgress progress)
    {
        Progress = progress;
    }

    public IProgress Progress { get; }

    public MessageType Type => MessageType.ProgressAdded;
}
