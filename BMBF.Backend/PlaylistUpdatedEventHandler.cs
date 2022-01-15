using BMBF.Backend.Models;

namespace BMBF.Backend;

public delegate void PlaylistUpdatedEventHandler(Playlist playlist, bool infoUpdated, bool songsUpdated, bool coverUpdated);