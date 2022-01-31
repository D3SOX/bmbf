namespace BMBF.Backend.Models.Messages;

public class SongAdded : IMessage
{
    public SongAdded(Song song)
    {
        Song = song;
    }

    public MessageType Type => MessageType.SongAdded;

    public Song Song { get; set; }
}