//
// BansheeQuery.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Data;
using System.Text;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Query;

namespace Banshee.Query
{
    public static class BansheeQuery
    {
        public static QueryFieldSet FieldSet {
            get { return field_set; }
        }
        
        public static QueryField ArtistField {
            get { return field_set["artist"]; }
        }

        public static QueryField AlbumField {
            get { return field_set["album"]; }
        }

        private static QueryFieldSet field_set = new QueryFieldSet (
            new QueryField (
                "artist", Catalog.GetString ("Artist"), "CoreArtists.Name", true,
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("artist"), Catalog.GetString ("by"), Catalog.GetString ("artists"),
                "by", "artist", "artists"
            ),
            new QueryField (
                "album", Catalog.GetString ("Album"), "CoreAlbums.Title", true,
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("album"), Catalog.GetString ("on"), Catalog.GetString ("from"),
                "on", "album", "from", "albumtitle"
            ),
            new QueryField (
                "disc", Catalog.GetString ("Disc"), "CoreTracks.Disc", typeof(NaturalIntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("disc"), Catalog.GetString ("cd"), Catalog.GetString ("discnum"),
                "disc", "cd", "discnum"
            ),
            new QueryField (
                "title", Catalog.GetString ("Track Title"), "CoreTracks.Title", true,
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("title"), Catalog.GetString ("titled"), Catalog.GetString ("name"), Catalog.GetString ("named"),
                "title", "titled", "name", "named"
            ),
            new QueryField (
                "year", Catalog.GetString ("Year"), "CoreTracks.Year", typeof(YearQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("year"), Catalog.GetString ("released"), Catalog.GetString ("yr"),
                "year", "released", "yr"
            ),
            new QueryField (
                "rating", Catalog.GetString ("Rating"), "CoreTracks.Rating", typeof(RatingQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("rating"), Catalog.GetString ("stars"),
                "rating", "stars"
            ),
            new QueryField (
                "playcount", Catalog.GetString ("Play Count"), "CoreTracks.PlayCount", typeof(NaturalIntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("plays"), Catalog.GetString ("playcount"), Catalog.GetString ("listens"),
                "plays", "playcount", "numberofplays", "listens"
            ),
            new QueryField (
                "skipcount", Catalog.GetString ("Skip Count"), "CoreTracks.SkipCount", typeof(NaturalIntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("skips"), Catalog.GetString ("skipcount"),
                "skips", "skipcount"
            ),
            new QueryField (
                "filesize", Catalog.GetString ("File Size"), "CoreTracks.FileSize", typeof(FileSizeQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("size"), Catalog.GetString ("filesize"),
                "size", "filesize"
            ),
            new QueryField (
                "uri", Catalog.GetString ("File Location"), "CoreTracks.Uri",
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("uri"), Catalog.GetString ("path"), Catalog.GetString ("file"), Catalog.GetString ("location"),
                "uri", "path", "file", "location"
            ),
            new QueryField (
                "duration", Catalog.GetString ("Duration"), "CoreTracks.Duration", typeof(IntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("duration"), Catalog.GetString ("length"), Catalog.GetString ("time"),
                "duration", "length", "time"
            ),
            new QueryField (
                "mimetype", Catalog.GetString ("Mime Type"), "CoreTracks.MimeType {0} OR CoreTracks.Uri {0}",
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("type"), Catalog.GetString ("mimetype"), Catalog.GetString ("format"), Catalog.GetString ("ext"),
                "type", "mimetype", "format", "ext", "mime"
            ),
            new QueryField (
                "lastplayed", Catalog.GetString ("Last Played Date"), "CoreTracks.LastPlayedStamp", typeof(DateQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("lastplayed"), Catalog.GetString ("played"), Catalog.GetString ("playedon"),
                "lastplayed", "played", "playedon"
            ),
            new QueryField (
                "added", Catalog.GetString ("Imported Date"), "CoreTracks.DateAddedStamp", typeof(DateQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("added"), Catalog.GetString ("imported"), Catalog.GetString ("addedon"), Catalog.GetString ("dateadded"), Catalog.GetString ("importedon"),
                "added", "imported", "addedon", "dateadded", "importedon"
            ),
            new QueryField (
                "playlistid", Catalog.GetString ("Playlist"),
                "CoreTracks.TrackID {2} IN (SELECT TrackID FROM CorePlaylistEntries WHERE PlaylistID = {1})", typeof(PlaylistQueryValue),
                "playlistid", "playlist"
            ),
            new QueryField (
                "smartplaylistid", Catalog.GetString ("Smart Playlist"),
                "CoreTracks.TrackID {2} IN (SELECT TrackID FROM CoreSmartPlaylistEntries WHERE SmartPlaylistID = {1})", typeof(SmartPlaylistQueryValue),
                "smartplaylistid", "smartplaylist"
            )
        );
    }
}
