using BMBF.ModManagement;

namespace BMBF.Backend.Models.Messages;

public class ModAdded : IMessage
{
    public ModAdded(IMod mod)
    {
        Mod = mod;
    }

    public MessageType Type => MessageType.ModAdded;

    public IMod Mod { get; set; }
}
