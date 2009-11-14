//
// FileNamePattern.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;

using Banshee.Collection;
using Banshee.Configuration.Schema;

namespace Banshee.Base
{
    public static class FileNamePattern
    {
        public delegate string ExpandTokenHandler (TrackInfo track, object replace);
        public delegate string FilterHandler (string path);

        public static FilterHandler Filter;

        public struct Conversion
        {
            private readonly string token;
            private readonly string name;
            private readonly ExpandTokenHandler handler;

            private readonly string token_string;

            public Conversion (string token, string name, ExpandTokenHandler handler)
            {
                this.token = token;
                this.name = name;
                this.handler = handler;

                this.token_string = "%" + this.token + "%";
            }

            public string Token {
                get { return token; }
            }

            public string Name {
                get { return name; }
            }

            public ExpandTokenHandler Handler {
                get { return handler; }
            }

            public string TokenString {
                get { return token_string; }
            }
        }

        private static SortedList<string, Conversion> conversion_table;

        public static void AddConversion (string token, string name, ExpandTokenHandler handler)
        {
            conversion_table.Add (token, new Conversion (token, name, handler));
        }

        static FileNamePattern ()
        {
            conversion_table = new SortedList<string, Conversion> ();

            AddConversion ("track_artist", Catalog.GetString ("Track Artist"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayArtistName);
            });

            AddConversion ("album_artist", Catalog.GetString ("Album Artist"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName);
            });

            // Alias for %album_artist%
            AddConversion ("artist", Catalog.GetString ("Album Artist"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName);
            });

            AddConversion ("album_artist_initial", Catalog.GetString("Album Artist Initial"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumArtistName.Substring(0, 1));
            });

            AddConversion ("conductor", Catalog.GetString ("Conductor"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.Conductor);
            });

            AddConversion ("composer", Catalog.GetString ("Composer"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.Composer);
            });

            AddConversion ("genre", Catalog.GetString ("Genre"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayGenre);
            });

            AddConversion ("album", Catalog.GetString ("Album"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayAlbumTitle);
            });

            AddConversion ("title", Catalog.GetString ("Title"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.DisplayTrackTitle);
            });

            AddConversion ("year", Catalog.GetString ("Year"),
                delegate (TrackInfo t, object r) {
                    int year = t == null ? (int)r : t.Year;
                    return year > 0 ? String.Format ("{0}", year) : null;
            });

            AddConversion ("track_count", Catalog.GetString ("Count"),
                delegate (TrackInfo t, object r) {
                    int track_count = t == null ? (int)r : t.TrackCount;
                    return track_count > 0 ? String.Format ("{0:00}", track_count) : null;
            });

            AddConversion ("track_number", Catalog.GetString ("Number"),
                delegate (TrackInfo t, object r) {
                    int track_number = t == null ? (int)r : t.TrackNumber;
                    return track_number > 0 ? String.Format ("{0:00}", track_number) : null;
            });

            AddConversion ("track_count_nz", Catalog.GetString ("Count (unsorted)"),
                delegate (TrackInfo t, object r) {
                    int track_count = t == null ? (int)r : t.TrackCount;
                    return track_count > 0 ? String.Format ("{0}", track_count) : null;
            });

            AddConversion ("track_number_nz", Catalog.GetString ("Number (unsorted)"),
                delegate (TrackInfo t, object r) {
                    int track_number = t == null ? (int)r : t.TrackNumber;
                    return track_number > 0 ? String.Format ("{0}", track_number) : null;
            });

            AddConversion ("disc_count", Catalog.GetString ("Disc Count"),
                delegate (TrackInfo t, object r) {
                    int disc_count = t == null ? (int)r : t.DiscCount;
                    return disc_count > 0 ? String.Format ("{0}", disc_count) : null;
            });

            AddConversion ("disc_number", Catalog.GetString ("Disc Number"),
                delegate (TrackInfo t, object r) {
                    int disc_number = t == null ? (int)r : t.DiscNumber;
                    return disc_number > 0 ? String.Format ("{0}", disc_number) : null;
            });

            AddConversion ("grouping", Catalog.GetString ("Grouping"),
                delegate (TrackInfo t, object r) {
                    return Escape (t == null ? (string)r : t.Grouping);
            });

            AddConversion ("path_sep", Path.DirectorySeparatorChar.ToString (),
                delegate (TrackInfo t, object r) {
                    return Path.DirectorySeparatorChar.ToString ();
            });
        }

        public static IEnumerable<Conversion> PatternConversions {
            get { return conversion_table.Values; }
        }

        public static string DefaultFolder {
            get { return "%album_artist%%path_sep%%album%"; }
        }

        public static string DefaultFile {
            get { return "%track_number%. %title%"; }
        }

        public static string DefaultPattern {
            get { return CreateFolderFilePattern (DefaultFolder, DefaultFile); }
        }

        private static string [] suggested_folders = new string [] {
            DefaultFolder,
            "%album_artist%%path_sep%%album_artist% - %album%",
            "%album_artist%%path_sep%%album% (%year%)",
            "%album_artist% - %album%",
            "%album%",
            "%album_artist%"
        };

        public static string [] SuggestedFolders {
            get { return suggested_folders; }
        }

        private static string [] suggested_files = new string [] {
            DefaultFile,
            "%track_number%. %track_artist% - %title%",
            "%track_artist% - %title%",
            "%track_artist% - %track_number% - %title%",
            "%track_artist% (%album%) - %track_number% - %title%",
            "%title%"
        };

        public static string [] SuggestedFiles {
            get { return suggested_files; }
        }

        private static string OnFilter (string input)
        {
            string repl_pattern = input;

            FilterHandler filter_handler = Filter;
            if (filter_handler != null) {
                repl_pattern = filter_handler (repl_pattern);
            }

            return repl_pattern;
        }

        public static string CreateFolderFilePattern (string folder, string file)
        {
            return String.Format ("{0}%path_sep%{1}", folder, file);
        }

        public static string CreatePatternDescription (string pattern)
        {
            pattern = Convert (pattern, conversion => conversion.Name);
            return OnFilter (pattern);
        }

        public static string CreateFromTrackInfo (TrackInfo track)
        {
            string pattern = null;

            try {
                pattern = CreateFolderFilePattern (
                    LibrarySchema.FolderPattern.Get (),
                    LibrarySchema.FilePattern.Get ()
                );
            } catch {
            }

            return CreateFromTrackInfo (pattern, track);
        }

        public static string CreateFromTrackInfo (string pattern, TrackInfo track)
        {
            if (pattern == null || pattern.Trim () == String.Empty) {
                pattern = DefaultPattern;
            }

            pattern = Convert (pattern, conversion => conversion.Handler (track, null));

            return OnFilter (pattern);
        }

        private static Regex optional_tokens_regex = new Regex ("{([^}]*)}", RegexOptions.Compiled);

        public static string Convert (string pattern, Func<Conversion, string> handler)
        {
            pattern = optional_tokens_regex.Replace (pattern, delegate (Match match) {
                var sub_pattern = match.Groups[1].Value;
                foreach (var conversion in PatternConversions) {
                    var token_string = conversion.TokenString;
                    if (!sub_pattern.Contains (token_string)) {
                        continue;
                    }
                    var replacement = handler (conversion);
                    if (String.IsNullOrEmpty (replacement)) {
                        sub_pattern = String.Empty;
                        break;
                    }
                    sub_pattern = sub_pattern.Replace (token_string, replacement);
                }
                return sub_pattern;
            });

            foreach (Conversion conversion in PatternConversions) {
                pattern = pattern.Replace (conversion.TokenString, handler (conversion));
            }

            return pattern;
        }

        public static string BuildFull (string base_dir, TrackInfo track)
        {
            return BuildFull (base_dir, track, Path.GetExtension (track.Uri.ToString ()));
        }

        public static string BuildFull (string base_dir, TrackInfo track, string ext)
        {
            if (ext == null || ext.Length < 1) {
                ext = String.Empty;
            } else if (ext[0] != '.') {
                ext = String.Format (".{0}", ext);
            }

            string songpath = CreateFromTrackInfo (track) + ext;
            songpath = Hyena.StringUtil.EscapePath (songpath);
            string dir = Path.GetFullPath (Path.Combine (base_dir,
                Path.GetDirectoryName (songpath)));
            string filename = Path.Combine (dir, Path.GetFileName (songpath));

            if (!Banshee.IO.Directory.Exists (dir)) {
                Banshee.IO.Directory.Create (dir);
            }

            return filename;
        }

        public static string Escape (string input)
        {
            return Hyena.StringUtil.EscapeFilename (input);
        }
    }
}
