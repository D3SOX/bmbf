namespace BMBF.Backend.Models.Messages;

public class InstallationUpdated : IMessage
{
    public InstallationUpdated(InstallationInfo? installation)
    {
        Installation = installation;
    }

    public MessageType Type => MessageType.InstallationUpdated;
        
    /// <summary>
    /// Null if Beat Saber is no longer installed
    /// </summary>
    public InstallationInfo? Installation { get; set; }
}