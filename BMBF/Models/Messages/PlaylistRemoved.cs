namespace BMBF.Models.Messages
{
    public class PlaylistRemoved : IMessage
    {
        public PlaylistRemoved(string id)
        {
            Id = id;
        }

        public MessageType Type => MessageType.PlaylistRemoved;

        public string Id { get; set; }
    }
}