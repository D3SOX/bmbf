using BMBF.Models;

namespace BMBF
{
    public delegate void PlaylistUpdatedEventHandler(Playlist playlist, bool infoUpdated, bool songsUpdated, bool coverUpdated);
}