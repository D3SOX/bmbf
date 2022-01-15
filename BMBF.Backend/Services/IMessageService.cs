namespace BMBF.Services;

/// <summary>
/// Manages creating and sending messages to the frontend for live updates.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Invoked whenever a message needs to be sent to the frontend
    /// </summary>
    event MessageEventHandler MessageSend;
}