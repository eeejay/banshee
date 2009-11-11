//
// ItunesPlayerImportSource.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2007 Scott Peterson
// Copyright (C) 2009 Alexander Kojevnikov
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
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.IO;
using Banshee.Library;
using Banshee.Playlist;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Widgets;
using Hyena.Data.Sqlite;

namespace Banshee.PlayerMigration
{
    public sealed class ItunesPlayerImportSource : ThreadPoolImportSource
    {
        // This is its own class so that we don't always load this stuff into memory
        private class ItunesImportData
        {
            public string library_uri, default_query, local_prefix, fallback_dir;
            public string[] query_dirs;
            public bool get_ratings, get_stats, get_playlists, user_provided_prefix, empty_library;
            public int total_songs, total_processed;
            public Dictionary<int, int> track_ids = new Dictionary<int, int> (); // key=itunes_id, value=banshee_id
        }

        private readonly object mutex = new object ();
        private volatile bool ok;

        public const string LibraryFilename = "iTunes Music Library.xml";

        public override string Name {
            get { return Catalog.GetString ("iTunes Media Player"); }
        }

        public override bool CanImport {
            get { return true; }
        }

        private ItunesImportData data;

        protected override bool ConfirmImport ()
        {
            if (data == null) {
                data = new ItunesImportData ();
                var dialog = new ItunesImportDialog ();
                if (!HandleImportDialog (dialog, delegate { data.library_uri = dialog.LibraryUri; })) {
                    data = null;
                    return false;
                }
            }
            return true;
        }

        private delegate void ImportDialogHandler (ItunesImportDialog dialog);

        private bool HandleImportDialog (ItunesImportDialog dialog, ImportDialogHandler code)
        {
            try {
                if (dialog.Run () == (int)ResponseType.Ok) {
                    if(code != null) {
                        code (dialog);
                    }
                    data.get_ratings = dialog.Ratings;
                    data.get_stats = dialog.Stats;
                    data.get_playlists = dialog.Playliststs;
                } else {
                    return false;
                }
            } finally {
                dialog.Destroy ();
                dialog.Dispose ();
            }

            if (String.IsNullOrEmpty (data.library_uri)) {
                return false;
            }

            // Make sure the library version is supported (version 1.1)
            string message = null;
            bool prompt = false;
            using (var xml_reader = new XmlTextReader (data.library_uri))
            {
                xml_reader.ReadToFollowing ("key");
                do {
                    xml_reader.Read ();
                    string key = xml_reader.ReadContentAsString ();
                    if (key == "Major Version" || key == "Minor Version") {
                        xml_reader.Read ();
                        xml_reader.Read ();
                        if(xml_reader.ReadContentAsString () != "1") {
                            message = Catalog.GetString (
                                "Banshee is not familiar with this version of the iTunes library format." +
                                " Importing may or may not work as expected, or at all. Would you like to attempt to import anyway?");
                            prompt = true;
                            break;
                        }
                    }
                } while (xml_reader.ReadToNextSibling ("key"));
            }

            if (prompt) {
                bool proceed = false;
                using (var message_dialog = new MessageDialog (null, 0, MessageType.Question, ButtonsType.YesNo, message)) {
                    if (message_dialog.Run () == (int)ResponseType.Yes) {
                        proceed = true;
                    }
                    message_dialog.Destroy ();
                }
                if (!proceed) {
                    LogError (data.library_uri, "Unsupported version");
                    return false;
                }
            }

            return true;
        }

        protected override void ImportCore ()
        {
            try {
                CountSongs ();
                data.empty_library = ServiceManager.SourceManager.MusicLibrary.TrackModel.Count == 0;

                var import_manager = ServiceManager.Get<LibraryImportManager> ();
                using (var xml_reader = new XmlTextReader (data.library_uri)) {
                    ProcessLibraryXml (import_manager, xml_reader);
                }
                import_manager.NotifyAllSources ();
            } finally {
                data = null;
            }
        }

        private void CountSongs ()
        {
            using (var xml_reader = new XmlTextReader (data.library_uri)) {
                xml_reader.ReadToDescendant("dict");
                xml_reader.ReadToDescendant("dict");
                xml_reader.ReadToDescendant("dict");
                do {
                    data.total_songs++;
                } while (xml_reader.ReadToNextSibling ("dict"));
            }
        }

        private void ProcessLibraryXml (LibraryImportManager import_manager, XmlReader xml_reader)
        {
            while (xml_reader.ReadToFollowing ("key") && !CheckForCanceled ()) {
                xml_reader.Read ();
                string key = xml_reader.ReadContentAsString ();
                xml_reader.Read ();
                xml_reader.Read ();

                switch (key) {
                case "Music Folder":
                    if (!ProcessMusicFolderPath (xml_reader.ReadContentAsString ())) {
                        return;
                    }
                    break;
                case "Tracks":
                    ProcessSongs (import_manager, xml_reader.ReadSubtree ());
                    break;
                case "Playlists":
                    if (data.get_playlists) {
                        ProcessPlaylists (xml_reader.ReadSubtree ());
                    }
                    break;
                }
            }
        }

        private bool ProcessMusicFolderPath(string path)
        {
            string[] itunes_music_uri_parts = ConvertToLocalUriFormat (path).Split (Path.DirectorySeparatorChar);
            string[] library_uri_parts = Path.GetDirectoryName (data.library_uri).Split (Path.DirectorySeparatorChar);

            string itunes_dir_name = library_uri_parts[library_uri_parts.Length - 1];
            int i = 0;
            bool found = false;

            for (i = itunes_music_uri_parts.Length - 1; i >= 0; i--) {
                if (itunes_music_uri_parts[i] == itunes_dir_name) {
                    found = true;
                    break;
                }
            }

            if (!found) {
                var builder = new StringBuilder (path.Length - 17);
                for (int j = 3; j < itunes_music_uri_parts.Length; j++) {
                    string part = itunes_music_uri_parts[j];
                    builder.Append (part);
                    if (part.Length > 0) {
                        builder.Append (Path.DirectorySeparatorChar);
                    }
                }

                string local_path = builder.ToString ();

                System.Threading.Monitor.Enter (mutex);

                ThreadAssist.ProxyToMain (delegate {
                    System.Threading.Monitor.Enter (mutex);
                    using (var dialog = new ItunesMusicDirectoryDialog (local_path)) {
                        if (dialog.Run () == (int)ResponseType.Ok) {
                            data.local_prefix = dialog.UserMusicDirectory;
                            data.user_provided_prefix = true;
                            data.default_query = local_path;
                            ok = true;
                        } else {
                            ok = false;
                        }
                        dialog.Destroy ();
                        System.Threading.Monitor.Pulse (mutex);
                        System.Threading.Monitor.Exit (mutex);
                    }
                });

                System.Threading.Monitor.Wait (mutex);
                System.Threading.Monitor.Exit (mutex);

                if (ok) {
                    return true;
                } else {
                    LogError (data.library_uri, "Unable to locate iTunes directory from iTunes URI");
                    return false;
                }
            }

            string[] tmp_query_dirs = new string[itunes_music_uri_parts.Length];
            string upstream_uri;
            string tmp_upstream_uri = null;
            int step = 0;
            string root = Path.GetPathRoot (data.library_uri);
            bool same_root = library_uri_parts[0] == root.Split (Path.DirectorySeparatorChar)[0];
            do {
                upstream_uri = tmp_upstream_uri;
                tmp_upstream_uri = root;
                for (int j = same_root ? 1 : 0; j < library_uri_parts.Length - step - 1; j++) {
                    tmp_upstream_uri = Path.Combine (tmp_upstream_uri, library_uri_parts[j]);
                }
                tmp_upstream_uri = Path.Combine (tmp_upstream_uri, itunes_music_uri_parts[i - step]);
                data.fallback_dir = tmp_query_dirs[step] = itunes_music_uri_parts[i - step];
                step++;
            } while (Banshee.IO.Directory.Exists (tmp_upstream_uri));
            if (upstream_uri == null) {
                LogError (data.library_uri, "Unable to resolve iTunes URIs to local URIs");
                return false;
            }
            data.query_dirs = new string[step - 2];
            data.default_query = string.Empty;

            for (int j = step - 2; j >= 0; j--) {
                if (j > 0) {
                    data.query_dirs[j - 1] = tmp_query_dirs[j];
                }
                data.default_query += tmp_query_dirs[j] + Path.DirectorySeparatorChar;

            }

            data.local_prefix = string.Empty;
            for (int j = 0; j <= library_uri_parts.Length - step; j++) {
                data.local_prefix += library_uri_parts[j] + Path.DirectorySeparatorChar;
            }

            return true;
        }

        private void ProcessSongs (LibraryImportManager import_manager, XmlReader xml_reader)
        {
            using (xml_reader) {
                xml_reader.ReadToFollowing ("dict");
                while (xml_reader.ReadToFollowing ("dict") && !CheckForCanceled ()) {
                    ProcessSong (import_manager, xml_reader.ReadSubtree ());
                }
            }
        }

        private void ProcessPlaylists (XmlReader xml_reader)
        {
            using (xml_reader) {
                while(xml_reader.ReadToFollowing ("dict") && !CheckForCanceled ()) {
                    ProcessPlaylist (xml_reader.ReadSubtree ());
                }
            }
        }

        private void ProcessSong (LibraryImportManager import_manager, XmlReader xml_reader)
        {
            data.total_processed++;

            var itunes_id = 0;
            var title = String.Empty;
            var title_sort = String.Empty;
            var genre = String.Empty;
            var artist = String.Empty;
            var artist_sort = String.Empty;
            var album_artist = String.Empty;
            var album_artist_sort = String.Empty;
            var composer = String.Empty;
            var album = String.Empty;
            var album_sort = String.Empty;
            var grouping = String.Empty;
            var year = 0;
            var rating = 0;
            var play_count = 0;
            var track_number = 0;
            var date_added = DateTime.Now;
            var last_played = DateTime.MinValue;

            SafeUri uri = null;

            using (xml_reader) {
                while (xml_reader.ReadToFollowing ("key")) {
                    xml_reader.Read();
                    string key = xml_reader.ReadContentAsString ();
                    xml_reader.Read ();
                    xml_reader.Read ();

                    try {
                        switch (key) {
                        case "Track ID":
                            itunes_id = Int32.Parse (xml_reader.ReadContentAsString ());
                            break;
                        case "Name":
                            title = xml_reader.ReadContentAsString ();
                            break;
                        case "Sort Name":
                            title_sort = xml_reader.ReadContentAsString ();
                            break;
                        case "Genre":
                            genre = xml_reader.ReadContentAsString ();
                            break;
                        case "Artist":
                            artist = xml_reader.ReadContentAsString ();
                            break;
                        case "Sort Artist":
                            artist_sort = xml_reader.ReadContentAsString ();
                            break;
                        case "Album Artist":
                            album_artist = xml_reader.ReadContentAsString ();
                            break;
                        case "Sort Album Artist":
                            album_artist_sort = xml_reader.ReadContentAsString ();
                            break;
                        case "Composer":
                            composer = xml_reader.ReadContentAsString ();
                            break;
                        case "Album":
                            album = xml_reader.ReadContentAsString ();
                            break;
                        case "Sort Album":
                            album_sort = xml_reader.ReadContentAsString ();
                            break;
                        case "Grouping":
                            grouping = xml_reader.ReadContentAsString ();
                            break;
                        case "Year":
                            year = Int32.Parse (xml_reader.ReadContentAsString ());
                            break;
                        case "Rating":
                            rating = Int32.Parse (xml_reader.ReadContentAsString ()) / 20;
                            break;
                        case "Play Count":
                            play_count = Int32.Parse (xml_reader.ReadContentAsString ());
                            break;
                        case "Track Number":
                            track_number = Int32.Parse (xml_reader.ReadContentAsString ());
                            break;
                        case "Date Added":
                            date_added = DateTime.Parse (xml_reader.ReadContentAsString (),
                                DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal);
                            break;
                        case "Play Date UTC":
                            last_played = DateTime.Parse (xml_reader.ReadContentAsString (),
                                DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal);
                            break;
                        case "Location":
                            uri = ConvertToLocalUri (xml_reader.ReadContentAsString ());
                            break;
                        }
                    } catch {
                    }
                }
            }

            if (uri == null) {
                return;
            }

            UpdateUserJob (data.total_processed, data.total_songs, artist, title);

            try {
                DatabaseTrackInfo track = import_manager.ImportTrack (uri);

                if (track == null) {
                    LogError (SafeUri.UriToFilename (uri), Catalog.GetString ("Unable to import song."));
                    return;
                }

                if (!String.IsNullOrEmpty (title)) {
                    track.TrackTitle = title;
                }
                if (!String.IsNullOrEmpty (title_sort)) {
                    track.TrackTitleSort = title_sort;
                }
                if (!String.IsNullOrEmpty (artist)) {
                    track.ArtistName = artist;
                }
                if (!String.IsNullOrEmpty (artist_sort)) {
                    track.ArtistNameSort = artist_sort;
                }
                if (!String.IsNullOrEmpty (genre)) {
                    track.Genre = genre;
                }
                if (!String.IsNullOrEmpty (album_artist)) {
                    track.AlbumArtist = album_artist;
                }
                if (!String.IsNullOrEmpty (album_artist_sort)) {
                    track.AlbumArtistSort = album_artist_sort;
                }
                if (!String.IsNullOrEmpty (composer)) {
                    track.Composer = composer;
                }
                if (!String.IsNullOrEmpty (album)) {
                    track.AlbumTitle = album;
                }
                if (!String.IsNullOrEmpty (album_sort)) {
                    track.AlbumTitleSort = album_sort;
                }
                if (!String.IsNullOrEmpty (grouping)) {
                    track.Grouping = grouping;
                }
                if (year > 0) {
                    track.Year = year;
                }
                if (data.get_ratings && rating > 0 && rating <= 5) {
                    track.Rating = rating;
                }
                if (data.get_stats && play_count > 0) {
                    track.PlayCount = play_count;
                }
                if (track_number > 0) {
                    track.TrackNumber = track_number;
                }
                if (data.get_stats) {
                    track.DateAdded = date_added;
                }
                if (data.get_stats && last_played > DateTime.MinValue) {
                    track.LastPlayed = last_played;
                }

                data.track_ids.Add (itunes_id, track.TrackId);

                track.Save (false);
            } catch (Exception e) {
                LogError (SafeUri.UriToFilename (uri), e);
            }
        }

        private void ProcessPlaylist (XmlReader xml_reader)
        {
            string name = string.Empty;
            bool skip = false;
            bool processed = false;

            using (xml_reader) {
                while (xml_reader.ReadToFollowing ("key")) {
                    xml_reader.Read ();
                    string key = xml_reader.ReadContentAsString ();
                    xml_reader.Read ();

                    switch (key) {
                    case "Name":
                        xml_reader.Read ();
                        name = xml_reader.ReadContentAsString ();
                        if (name == "Library" ||
                            name == "Music Videos" ||
                            name == "Audiobooks" ||
                            name == "Music" ||
                            name == "Movies" ||
                            name == "Party Shuffle" ||
                            name == "Podcasts" ||
                            name == "Party Shuffle" ||
                            name == "Purchased Music" ||
                            name == "Genius" ||
                            name == "TV Shows") {
                            skip = true;
                        }
                        break;
                    case "Smart Info":
                        skip = true;
                        break;
                    case "Smart Criteria":
                        skip = true;
                        break;
                    case "Playlist Items":
                        xml_reader.Read ();
                        if(!skip) {
                            ProcessPlaylist (name, xml_reader.ReadSubtree ());
                            processed = true;
                        }
                        break;
                    }
                }
            }

            // Empty playlist
            if (!processed && !skip) {
                ProcessPlaylist (name, null);
            }
        }

        private void ProcessPlaylist (string name, XmlReader xml_reader)
        {
            UpdateUserJob (1, 1, Catalog.GetString("Playlists"), name);

            ProcessRegularPlaylist (name, xml_reader);
            if (xml_reader != null) {
                xml_reader.Close ();
            }
        }

        private void ProcessRegularPlaylist (string name, XmlReader xml_reader)
        {
            var playlist_source = new PlaylistSource (name, ServiceManager.SourceManager.MusicLibrary);
            playlist_source.Save ();
            ServiceManager.SourceManager.MusicLibrary.AddChildSource (playlist_source);

            // Get the songs in the playlists
            if (xml_reader != null) {
                while (xml_reader.ReadToFollowing ("integer") && !CheckForCanceled ()) {
                    xml_reader.Read ();
                    int itunes_id = Int32.Parse (xml_reader.ReadContentAsString ());
                    int track_id;
                    if (data.track_ids.TryGetValue (itunes_id, out track_id)) {
                        try {
                            ServiceManager.DbConnection.Execute (
                                "INSERT INTO CorePlaylistEntries (PlaylistID, TrackID) VALUES (?, ?)",
                                playlist_source.DbId, track_id);
                        } catch {
                        }
                    }
                }
                playlist_source.Reload ();
                playlist_source.NotifyUser ();
            }
        }

        private SafeUri ConvertToLocalUri (string raw_uri)
        {
            if (raw_uri == null) {
                return null;
            }

            string uri = ConvertToLocalUriFormat (raw_uri);
            int index = uri.IndexOf (data.default_query);

            if (data.user_provided_prefix && index != -1) {
                index += data.default_query.Length;
            } else if (index == -1 && data.query_dirs.Length > 0) {
                int count = 0;
                string path = data.query_dirs[data.query_dirs.Length - 1];
                do {
                    for (int k = data.query_dirs.Length - 2; k >= count; k--) {
                        path = Path.Combine (path, data.query_dirs[k]);
                    }
                    index = uri.IndexOf (path);
                    count++;
                } while(index == -1 && count < data.query_dirs.Length);
                if (index == -1) {
                    index = uri.IndexOf(data.fallback_dir);
                    if (index != -1) {
                        index += data.fallback_dir.Length + 1;
                    }
                }
            }

            if (index == -1) {
                if (data.empty_library) {
                    LogError (uri, "Unable to map iTunes URI to local URI");
                }
                return null;
            }
            SafeUri safe_uri = CreateSafeUri (Path.Combine(
                data.local_prefix, uri.Substring (index, uri.Length - index)), data.empty_library);

            if (safe_uri == null && !data.empty_library) {
                string local_uri = string.Empty;
                string lower_uri = raw_uri.ToLower (CultureInfo.InvariantCulture);
                int i = lower_uri.Length;
                while (true) {
                    i = lower_uri.LastIndexOf (Path.DirectorySeparatorChar, i - 1);
                    if (i == -1) {
                        break;
                    }
                    try {
                        using (var reader = ServiceManager.DbConnection.Query (String.Format (
                            @"SELECT Uri FROM CoreTracks WHERE lower(Uri) LIKE ""%{0}""", lower_uri.Substring (i + 1)))) {
                            bool found = false;
                            local_uri = string.Empty;
                            while (reader.Read ()) {
                                if (found) {
                                    local_uri = string.Empty;
                                    break;
                                }
                                found = true;
                                local_uri = (string)reader[0];
                            }
                            if (!found || local_uri.Length > 0) {
                                break;
                            }
                        }
                    } catch {
                        break;
                    }
                }
                if (local_uri.Length > 0) {
                    safe_uri = CreateSafeUri (local_uri, true);
                } else {
                    LogError (uri, "Unable to map iTunes URI to local URI");
                }
            }

            return safe_uri;
        }

        private SafeUri CreateSafeUri (string uri, bool complain)
        {
            SafeUri safe_uri;
            try {
                safe_uri = new SafeUri (uri);
            } catch {
                if (complain) {
                    LogError (uri, "URI is not a local file path");
                }
                return null;
            }

            safe_uri = FindFile (safe_uri);
            if (safe_uri == null) {
                if (complain) {
                    LogError (uri, "File does not exist");
                }
                return null;
            }

            return safe_uri;
        }

        // URIs are UTF-8 percent-encoded. Deconding with System.Web.HttpServerUtility
        // involves too much overhead, so we do it cheap here.
        private static string ConvertToLocalUriFormat (string input)
        {
            var builder = new StringBuilder (input.Length);
            byte[] buffer = new byte[2];
            bool using_buffer = false;
            for (int i = 0; i < input.Length; i++) {
                // If it's a '%', treat the two subsiquent characters as a UTF-8 byte in hex.
                if (input[i] == '%') {
                    byte code = Byte.Parse (input.Substring(i + 1, 2),
                        System.Globalization.NumberStyles.HexNumber);
                    // If it's a non-ascii character, or there are already some non-ascii
                    // characters in the buffer, then queue it for UTF-8 decoding.
                    if (using_buffer || (code & 0x80) != 0) {
                        if (using_buffer) {
                            if (buffer[1] == 0) {
                                buffer[1] = code;
                            } else {
                                byte[] new_buffer = new byte[buffer.Length + 1];
                                for (int j = 0; j < buffer.Length; j++) {
                                    new_buffer[j] = buffer[j];
                                }
                                buffer = new_buffer;
                                buffer[buffer.Length - 1] = code;
                            }
                        } else {
                            buffer[0] = code;
                            using_buffer = true;
                        }
                    }
                        // If it's a lone ascii character, there's no need for fancy UTF-8 decoding.
                    else {
                        builder.Append ((char)code);
                    }
                    i += 2;
                } else {
                    // If we have something in the buffer, decode it.
                    if (using_buffer) {
                        builder.Append (Encoding.UTF8.GetString (buffer));
                        if (buffer.Length > 2) {
                            buffer = new byte[2];
                        } else {
                            buffer[1] = 0;
                        }
                        using_buffer = false;
                    }
                    // And add our regular characters and convert to local directory separator char.
                    if (input[i] == '/') {
                        builder.Append (Path.DirectorySeparatorChar);
                    } else {
                        builder.Append (input[i]);
                    }
                }
            }
            return builder.ToString ();
        }

        private static SafeUri FindFile (SafeUri uri)
        {
            // URIs kept by iTunes often contain characters in a case different from the actual
            // files and directories. This method tries to find the real file URI.

            if (Banshee.IO.File.Exists (uri)) {
                return uri;
            }

            string path = uri.AbsolutePath;
            string file = Path.GetFileName (path);
            string directory = Path.GetDirectoryName (path);
            directory = FindDirectory (directory);
            if (directory == null) {
                return null;
            }

            uri = new SafeUri (Path.Combine (directory, file), false);
            if (Banshee.IO.File.Exists (uri)) {
                return uri;
            }

            foreach (string item in Banshee.IO.Directory.GetFiles (directory)) {
                string name = Path.GetFileName (item);
                if (0 != String.Compare (file, name, true)) {
                    continue;
                }
                return new SafeUri (Path.Combine (directory, name), false);
            }

            return null;
        }

        private static string FindDirectory (string directory)
        {
            if (Banshee.IO.Directory.Exists (directory)) {
                return directory;
            }

            string current = Path.GetFileName (directory);
            directory = Path.GetDirectoryName (directory);
            if (String.IsNullOrEmpty (directory)) {
                return null;
            }

            directory = FindDirectory (directory);
            if (String.IsNullOrEmpty (directory)) {
                return null;
            }

            foreach (string item in Banshee.IO.Directory.GetDirectories (directory)) {
                string name = Path.GetFileName (item);
                if (0 != String.Compare (current, name, true)) {
                    continue;
                }
                return Path.Combine (directory, name);
            }

            return null;
        }

        public override string [] IconNames {
            get { return new string [] { "itunes", "system-search" }; }
        }

        public override int SortOrder {
            get { return 40; }
        }
    }
}
