using System;
using Banshee.Sources;

namespace Banshee.Base
{
    public interface IPlaylistCapable
    {
        DapPlaylistSource AddPlaylist(Source playlist);
    }
}
