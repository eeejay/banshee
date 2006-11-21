using System;
using KarmaLib=Karma;
using Banshee.Base;
using Banshee.Dap;

namespace Banshee.Dap.Karma
{
    public sealed class KarmaDapTrackInfo : DapTrackInfo
    {
        public KarmaDapTrackInfo(KarmaLib.Song song, string mount)
        {
            string fidstr = String.Format("{0:x8}", song.Id);
            uri = new SafeUri(String.Format("file://{0}/fids0/_{1}/{2}", mount,
                fidstr.Substring(0,5), fidstr.Substring(5)));
            album = song.Album;
            artist = song.Artist;
            title = song.Title;
            genre = song.Genre;
            track_id = song.Id;
            duration = new TimeSpan(song.Duration * 1000L);
            play_count = song.PlayCount;
            last_played = song.LastPlayed;
            track_count = 0;
            track_number = song.TrackNumber;
            year = song.Year;
        }
    }
}
