namespace BMBF.Models.Messages;

public interface IMessage
{
    /// <summary>
    /// Type of the message, to tell the frontend how to handle this message.
    /// </summary>
    MessageType Type { get; }
}