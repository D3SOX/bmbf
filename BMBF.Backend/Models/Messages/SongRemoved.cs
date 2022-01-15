namespace BMBF.Backend.Models.Messages;

public class SongRemoved : IMessage
{
    public SongRemoved(string hash)
    {
        Hash = hash;
    }

    public MessageType Type => MessageType.SongRemoved;
        
    public string Hash { get; set; }
}