//
// XspfPlaylistFormat.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using Mono.Unix;

using Banshee.Collection;
using Banshee.Sources;
using Xspf = Media.Playlists.Xspf;

namespace Banshee.Playlists.Formats
{
    public class XspfPlaylistFormat : PlaylistFormatBase
    {
        public static readonly PlaylistFormatDescription FormatDescription = new PlaylistFormatDescription (
            typeof (XspfPlaylistFormat),
            MagicHandler,
            Catalog.GetString ("XML Shareable Playlist Format version 1 (*.xspf)"),
            "xspf",
            new string [] {"application/xspf+xml"}
        );

        public static bool MagicHandler (StreamReader stream)
        {
            try {
                return Xspf.Playlist.Sniff (stream);
            } catch {
                return false;
            }
        }

        public XspfPlaylistFormat ()
        {
        }

        public override void Load (StreamReader stream, bool validateHeader)
        {
            Xspf.Playlist playlist = new Xspf.Playlist ();
            playlist.DefaultBaseUri = BaseUri;
            playlist.Load (stream);
            Title = playlist.Title;
            foreach (Xspf.Track track in playlist.Tracks) {
                Dictionary<string, object> element = AddElement ();
                element["uri"] = track.GetLocationAt (0);
            }
        }

        public override void Save (Stream stream, ITrackModelSource source)
        {
            Xspf.Playlist playlist = new Xspf.Playlist ();
            playlist.Title = source.Name;
            playlist.Date = DateTime.Now;
            for (int ii = 0; ii < source.TrackModel.Count; ii++) {
                TrackInfo track = source.TrackModel[ii];
                Xspf.Track xtrack = new Xspf.Track ();
                xtrack.AddLocation (new Uri (ExportUri (track.Uri), UriKind.RelativeOrAbsolute));
                xtrack.Title = track.TrackTitle;
                xtrack.Creator = track.ArtistName;
                xtrack.Album = track.AlbumTitle;
                if (track.TrackNumber >= 0) {
                    xtrack.TrackNumber = (uint)track.TrackNumber;
                }
                xtrack.Duration = track.Duration;
                playlist.AddTrack (xtrack);
            }
            playlist.Save (stream);
        }
    }
}
// vi:tabstop=4:expandtab
