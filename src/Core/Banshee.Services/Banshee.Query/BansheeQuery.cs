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
    /*public interface IQueryDefines
    {
        QueryOrder [] Orders { get; }
        QueryLimit [] Limits { get; }
        QueryFieldSet FieldSet { get; }
        string GetSqlSort (string key, bool asc);
    }
    
    public class QueryDefines : IQueryDefines
    {
        
    }*/
    
    public static class BansheeQuery
    {
        private static bool asc = true;
        private static bool desc = false;

        public static QueryOrder RandomOrder = CreateQueryOrder ("Random",     asc,  Catalog.GetString ("Random"), null);

        public static QueryOrder [] Orders = new QueryOrder [] {
            RandomOrder,
            CreateQueryOrder ("Album",      asc,  Catalog.GetString ("Album"), AlbumField),
            CreateQueryOrder ("Artist",     asc,  Catalog.GetString ("Artist"), ArtistField),
            CreateQueryOrder ("Title",      asc,  Catalog.GetString ("Title"), TitleField),
            CreateQueryOrder ("Genre",      asc,  Catalog.GetString ("Genre"), GenreField),
            null,
            CreateQueryOrder ("Rating",     desc, Catalog.GetString ("Highest Rating"), RatingField),
            CreateQueryOrder ("Rating",     asc,  Catalog.GetString ("Lowest Rating"), RatingField),
            null,
            CreateQueryOrder ("PlayCount",  desc, Catalog.GetString ("Most Often Played"), PlayCountField),
            CreateQueryOrder ("PlayCount",  asc,  Catalog.GetString ("Least Often Played"), PlayCountField),
            null,
            CreateQueryOrder ("LastPlayedStamp", desc, Catalog.GetString ("Most Recently Played"), LastPlayedField),
            CreateQueryOrder ("LastPlayedStamp", asc,  Catalog.GetString ("Least Recently Played"), LastPlayedField),
            null,
            CreateQueryOrder ("DateAddedStamp",  desc, Catalog.GetString ("Most Recently Added"), DateAddedField),
            CreateQueryOrder ("DateAddedStamp",  asc,  Catalog.GetString ("Least Recently Added"), DateAddedField)
        };

        public static QueryLimit [] Limits = new QueryLimit [] {
            new QueryLimit ("songs",   Catalog.GetString ("items"), true),
            new QueryLimit ("minutes", Catalog.GetString ("minutes"), "CoreTracks.Duration/1000", (int) TimeFactor.Minute),
            new QueryLimit ("hours",   Catalog.GetString ("hours"), "CoreTracks.Duration/1000", (int) TimeFactor.Hour),
            new QueryLimit ("MB",      Catalog.GetString ("MB"), "CoreTracks.FileSize", (int) FileSizeFactor.MB),
            new QueryLimit ("GB",      Catalog.GetString ("GB"), "CoreTracks.FileSize", (int) FileSizeFactor.GB)
        };
        
#region QueryField Definitions

        public static QueryField ArtistField = new QueryField (
            "artist", Catalog.GetString ("Artist"), "CoreArtists.NameLowered", true,
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("artist"), Catalog.GetString ("by"), Catalog.GetString ("artists"),
            "by", "artist", "artists"
        );

        public static QueryField AlbumField = new QueryField (
            "album", Catalog.GetString ("Album"), "CoreAlbums.TitleLowered", true,
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("album"), Catalog.GetString ("on"), Catalog.GetString ("from"),
            "on", "album", "from", "albumtitle"
        );

        public static QueryField DiscField = new QueryField (
            "disc", Catalog.GetString ("Disc"), "CoreTracks.Disc", typeof(NaturalIntegerQueryValue),
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("disc"), Catalog.GetString ("cd"), Catalog.GetString ("discnum"),
            "disc", "cd", "discnum"
        );

        public static QueryField TitleField = new QueryField (
            "title", Catalog.GetString ("Track Title"), "CoreTracks.TitleLowered", true,
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("title"), Catalog.GetString ("titled"), Catalog.GetString ("name"), Catalog.GetString ("named"),
            "title", "titled", "name", "named"
        );

        public static QueryField YearField = new QueryField (
            "year", Catalog.GetString ("Year"), "CoreTracks.Year", typeof(YearQueryValue),
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("year"), Catalog.GetString ("released"), Catalog.GetString ("yr"),
            "year", "released", "yr"
        );

        public static QueryField GenreField = new QueryField (
            "genre", Catalog.GetString ("Genre"), "CoreTracks.Genre", false,
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("genre"), "genre"
        );

        public static QueryField ComposerField = new QueryField (
            "composer", Catalog.GetString ("Composer"), "CoreTracks.Composer", false,
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("composer"), "composer"
        );

        public static QueryField RatingField = new QueryField (
            "rating", Catalog.GetString ("Rating"), "CoreTracks.Rating", new Type [] {typeof(RatingQueryValue)},//, typeof(NullQueryValue)},
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("rating"), Catalog.GetString ("stars"),
            "rating", "stars"
        );

        public static QueryField PlayCountField = new QueryField (
            "playcount", Catalog.GetString ("Play Count"), "CoreTracks.PlayCount", typeof(NaturalIntegerQueryValue),
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("plays"), Catalog.GetString ("playcount"), Catalog.GetString ("listens"),
            "plays", "playcount", "numberofplays", "listens"
        );

        public static QueryField SkipCountField = new QueryField (
            "skipcount", Catalog.GetString ("Skip Count"), "CoreTracks.SkipCount", typeof(NaturalIntegerQueryValue),
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("skips"), Catalog.GetString ("skipcount"),
            "skips", "skipcount"
        );

        public static QueryField FileSizeField = new QueryField (
            "filesize", Catalog.GetString ("File Size"), "CoreTracks.FileSize", typeof(FileSizeQueryValue),
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("size"), Catalog.GetString ("filesize"),
            "size", "filesize"
        );

        public static QueryField UriField = new QueryField (
            "uri", Catalog.GetString ("File Location"), "CoreTracks.Uri",
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("uri"), Catalog.GetString ("path"), Catalog.GetString ("file"), Catalog.GetString ("location"),
            "uri", "path", "file", "location"
        );

        public static QueryField DurationField = new QueryField (
            "duration", Catalog.GetString ("Duration"), "CoreTracks.Duration", typeof(TimeSpanQueryValue),
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("duration"), Catalog.GetString ("length"), Catalog.GetString ("time"),
            "duration", "length", "time"
        );

        public static QueryField MimeTypeField = new QueryField (
            "mimetype", Catalog.GetString ("Mime Type"), "CoreTracks.MimeType {0} OR CoreTracks.Uri {0}",
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("type"), Catalog.GetString ("mimetype"), Catalog.GetString ("format"), Catalog.GetString ("ext"),
            "type", "mimetype", "format", "ext", "mime"
        );

        public static QueryField LastPlayedField = new QueryField (
            "lastplayed", Catalog.GetString ("Last Played Date"), "CoreTracks.LastPlayedStamp", new Type [] {typeof(RelativeTimeSpanQueryValue), typeof(DateQueryValue)},
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("lastplayed"), Catalog.GetString ("played"), Catalog.GetString ("playedon"),
            "lastplayed", "played", "playedon"
        );

        public static QueryField LastSkippedField = new QueryField (
            "lastskipped", Catalog.GetString ("Last Skipped Date"), "CoreTracks.LastSkippedStamp", new Type [] {typeof(RelativeTimeSpanQueryValue), typeof(DateQueryValue)},
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("lastskipped"), Catalog.GetString ("skipped"), Catalog.GetString ("skippedon"),
            "lastskipped", "skipped", "skippedon"
        );

        public static QueryField DateAddedField = new QueryField (
            "added", Catalog.GetString ("Date Added"), "CoreTracks.DateAddedStamp", new Type [] {typeof(RelativeTimeSpanQueryValue), typeof(DateQueryValue)},
            // Translators: These are unique search fields.  Please, no spaces. Blank ok.
            Catalog.GetString ("added"), Catalog.GetString ("imported"), Catalog.GetString ("addedon"), Catalog.GetString ("dateadded"), Catalog.GetString ("importedon"),
            "added", "imported", "addedon", "dateadded", "importedon"
        );

        public static QueryField PlaylistField = new QueryField (
            "playlistid", Catalog.GetString ("Playlist"),
            "CoreTracks.TrackID {2} IN (SELECT TrackID FROM CorePlaylistEntries WHERE PlaylistID = {1})", typeof(PlaylistQueryValue),
            "playlistid", "playlist"
        );

        public static QueryField SmartPlaylistField = new QueryField (
            "smartplaylistid", Catalog.GetString ("Smart Playlist"),
            "CoreTracks.TrackID {2} IN (SELECT TrackID FROM CoreSmartPlaylistEntries WHERE SmartPlaylistID = {1})", typeof(SmartPlaylistQueryValue),
            "smartplaylistid", "smartplaylist"
        );
        
#endregion

        public static QueryFieldSet FieldSet = new QueryFieldSet (
            ArtistField, AlbumField, DiscField, TitleField, YearField, GenreField, ComposerField, RatingField, PlayCountField,
            SkipCountField, FileSizeField, UriField, DurationField, MimeTypeField, LastPlayedField, LastSkippedField,
            DateAddedField, PlaylistField, SmartPlaylistField
        );

        private const string default_sort = @"CoreAlbums.ArtistNameLowered ASC, CoreAlbums.TitleLowered ASC, CoreTracks.Disc ASC, CoreTracks.TrackNumber ASC";
        public static string GetSort (string key)
        {
            return GetSort (key, false);
        }

        public static string GetSort (string key, bool asc)
        {
            string ascDesc = asc ? "ASC" : "DESC";
            string sort_query = null;
            switch(key) {
                case "Track":
                    sort_query = String.Format (@"
                        CoreAlbums.ArtistNameLowered ASC, 
                        CoreAlbums.TitleLowered ASC, 
                        CoreTracks.Disc ASC,
                        CoreTracks.TrackNumber {0}", ascDesc); 
                    break;

                case "Artist":
                    sort_query = String.Format (@"
                        CoreArtists.NameLowered {0}, 
                        CoreAlbums.TitleLowered ASC,
                        CoreTracks.Disc ASC,
                        CoreTracks.TrackNumber ASC", ascDesc); 
                    break;

                case "Album":
                    sort_query = String.Format (@"
                        CoreAlbums.TitleLowered {0},
                        CoreTracks.Disc ASC,
                        CoreTracks.TrackNumber ASC", ascDesc); 
                    break;

                case "Title":
                    sort_query = String.Format (@"
                        CoreTracks.TitleLowered {0},
                        CoreAlbums.ArtistNameLowered ASC, 
                        CoreAlbums.TitleLowered ASC", ascDesc); 
                    break;

                case "Random":
                    sort_query = "RANDOM ()";
                    break;

                case "Year":
                case "Genre":
                case "Disc":
                case "Duration":
                case "Rating":
                case "PlayCount":
                case "SkipCount":
                case "FileSize":
                case "LastPlayedStamp":
                case "LastSkippedStamp":
                case "DateAddedStamp":
                case "Uri":
                case "Composer":
                    sort_query = String.Format (
                        "CoreTracks.{0} {1}, {2}",
                        key, ascDesc, default_sort
                    );
                    break;
            }
            return sort_query;
        }

        private static QueryOrder CreateQueryOrder (string name, bool asc, string label, QueryField field)
        {
            return new QueryOrder (CreateOrderName (name, asc), label, GetSort (name, asc), field);
        }

        public static QueryLimit FindLimit (string name)
        {
            foreach (QueryLimit limit in Limits) {
                if (limit.Name == name)
                    return limit;
            }
            return null;
        }

        public static QueryOrder FindOrder (string name, bool asc)
        {
            return FindOrder (CreateOrderName (name, asc));
        }

        public static QueryOrder FindOrder (string name)
        {
            foreach (QueryOrder order in Orders) {
                if (order != null && order.Name == name) {
                    return order;
                }
            }
            return null;
        }

        private static string CreateOrderName (string name, bool asc)
        {
            return String.Format ("{0}-{1}", name, asc ? "ASC" : "DESC");
        }

        static BansheeQuery () {
            // Set translated names for operators
            IntegerQueryValue.Equal.Label            = Catalog.GetString ("is");
            IntegerQueryValue.NotEqual.Label         = Catalog.GetString ("is not");
            IntegerQueryValue.LessThanEqual.Label    = Catalog.GetString ("at most");
            IntegerQueryValue.GreaterThanEqual.Label = Catalog.GetString ("at least");
            IntegerQueryValue.LessThan.Label         = Catalog.GetString ("less than");
            IntegerQueryValue.GreaterThan.Label      = Catalog.GetString ("more than");

            //DateQueryValue.Equal.Label               = Catalog.GetString ("is");
            //DateQueryValue.NotEqual.Label            = Catalog.GetString ("is not");
            //DateQueryValue.LessThanEqual.Label       = Catalog.GetString ("at most");
            //DateQueryValue.GreaterThanEqual.Label    = Catalog.GetString ("at least");
            DateQueryValue.LessThan.Label            = Catalog.GetString ("before");
            DateQueryValue.GreaterThan.Label         = Catalog.GetString ("after");

            RelativeTimeSpanQueryValue.GreaterThan.Label         = Catalog.GetString ("more than");
            RelativeTimeSpanQueryValue.LessThan.Label            = Catalog.GetString ("less than");
            RelativeTimeSpanQueryValue.GreaterThanEqual.Label    = Catalog.GetString ("at least");
            RelativeTimeSpanQueryValue.LessThanEqual.Label       = Catalog.GetString ("at most");

            StringQueryValue.Equal.Label             = Catalog.GetString ("is");
            StringQueryValue.NotEqual.Label          = Catalog.GetString ("is not");
            StringQueryValue.Contains.Label          = Catalog.GetString ("contains");
            StringQueryValue.DoesNotContain.Label    = Catalog.GetString ("doesn't contain");
            StringQueryValue.StartsWith.Label        = Catalog.GetString ("starts with");
            StringQueryValue.EndsWith.Label          = Catalog.GetString ("ends with");
        }
    }
}
