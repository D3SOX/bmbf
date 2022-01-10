namespace BMBF.Models.Messages
{
    public class PlaylistAdded : IMessage
    {
        public PlaylistAdded(PlaylistInfo playlistInfo)
        {
            PlaylistInfo = playlistInfo;
        }

        public MessageType Type => MessageType.PlaylistAdded;

        public PlaylistInfo PlaylistInfo { get; set; }
    }
}