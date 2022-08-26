namespace BMBF.Backend.Models.Messages;

public class SetupQuit : IMessage
{
    public SetupQuit(bool isFinished)
    {
        IsFinished = isFinished;
    }

    public MessageType Type => MessageType.SetupQuit;

    /// <summary>
    /// True if setup quit due to setup finishing, false if setup quit before completion.
    /// </summary>
    public bool IsFinished { get; }
}
