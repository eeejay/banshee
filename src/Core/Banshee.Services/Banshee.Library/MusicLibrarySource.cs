//
// MusicLibrarySource.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Collections.Generic;

using Mono.Unix;

using Banshee.SmartPlaylist;

namespace Banshee.Library
{
    public class MusicLibrarySource : LibrarySource
    {
        public MusicLibrarySource () : base (Catalog.GetString ("Music Library"), "Library", 20)
        {
            Properties.SetStringList ("Icon.Name", "audio-x-generic", "source-library");
        }

        public override IEnumerable<SmartPlaylistDefinition> DefaultSmartPlaylists {
            get { return default_smart_playlists; }
        }

        public override IEnumerable<SmartPlaylistDefinition> NonDefaultSmartPlaylists {
            get { return non_default_smart_playlists; }
        }

        private static SmartPlaylistDefinition [] default_smart_playlists = new SmartPlaylistDefinition [] {
            new SmartPlaylistDefinition (
                Catalog.GetString ("Favorites"),
                Catalog.GetString ("Songs rated four and five stars"),
                "rating>=4"),

            new SmartPlaylistDefinition (
                Catalog.GetString ("Recent Favorites"),
                Catalog.GetString ("Songs listened to often in the past week"),
                "played<\"1 week ago\" playcount>3"),

            new SmartPlaylistDefinition (
                Catalog.GetString ("Recently Added"),
                Catalog.GetString ("Songs imported within the last week"),
                "added<\"1 week ago\""),

            new SmartPlaylistDefinition (
                Catalog.GetString ("Unheard"),
                Catalog.GetString ("Songs that have not been played or skipped"),
                "playcount:0 skips:0"),
        };

        private static SmartPlaylistDefinition [] non_default_smart_playlists = new SmartPlaylistDefinition [] {
            new SmartPlaylistDefinition (
                Catalog.GetString ("Neglected Favorites"),
                Catalog.GetString ("Favorites not played in over two weeks"),
                "rating>=4 played>=\"2 months ago\""),

            new SmartPlaylistDefinition (
                Catalog.GetString ("Least Favorite"),
                Catalog.GetString ("Songs rated one or two stars or that you have frequently skipped"),
                "rating<3 or skips>4"),

            new SmartPlaylistDefinition (
                Catalog.GetString ("700 MB of Favorites"),
                Catalog.GetString ("A data CD worth of favorite songs"),
                "rating>=4",
                700, "MB", "PlayCount-DESC"),

            new SmartPlaylistDefinition (
                Catalog.GetString ("80 Minutes of Favorites"),
                Catalog.GetString ("An audio CD worth of favorite songs"),
                "rating>=4",
                80, "minutes", "PlayCount-DESC"),

            new SmartPlaylistDefinition (
                Catalog.GetString ("Unrated"),
                Catalog.GetString ("Songs that haven't been rated"),
                "rating=0"),
        };
    }
}
