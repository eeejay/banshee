using System;
using KarmaLib=Karma;
using Banshee.Base;
using Banshee.Collection.Database;

namespace Banshee.Dap.Karma
{
    public class KarmaTrackInfo : DatabaseTrackInfo
    {
        private int karma_id;

        public KarmaTrackInfo(KarmaLib.Song song, string mount)
        {
            string fidstr = String.Format("{0:x8}", song.Id);
            Uri = new SafeUri(String.Format("file://{0}/fids0/_{1}/{2}", mount,
                fidstr.Substring(0,5), fidstr.Substring(5)));
            karma_id = song.Id;
            AlbumTitle = song.Album;
            ArtistName = song.Artist;
            TrackTitle = song.Title;
            Genre = song.Genre;
            Duration = new TimeSpan(song.Duration * 1000L);
            PlayCount = (int) song.PlayCount;
            LastPlayed = song.LastPlayed;
            TrackCount = 0;
            TrackNumber = (int) song.TrackNumber;
            Year = song.Year;
        }

        public int KarmaId {
            get { return karma_id; }
        }
    }
}
