//
// DefaultColumnController.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Mono.Unix;

using Hyena.Gui;
using Hyena.Gui.Theming;
using Hyena.Gui.Theatrics;
using Hyena.Data.Gui;

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

namespace Banshee.Collection.Gui
{
    public class DefaultColumnController : PersistentColumnController
    {
        public DefaultColumnController () : this (true)
        {
        }
        
        public DefaultColumnController (bool loadDefault) : base (String.Format ("{0}.{1}", 
            Application.ActiveClient.ClientId, "track_view_columns"))
        {
            if (loadDefault) {
                AddDefaultColumns ();
                DefaultSortColumn = ArtistColumn;
                Load ();
            }
        }
        
        public void AddDefaultColumns ()
        {
            AddRange (
                IndicatorColumn,
                TrackColumn,
                TitleColumn,
                ArtistColumn,
                AlbumColumn,
                RatingColumn,
                DurationColumn,
                GenreColumn,
                YearColumn,
                FileSizeColumn,
                ComposerColumn,
                PlayCountColumn,
                SkipCountColumn,
                DiscColumn,
                LastPlayedColumn,
                LastSkippedColumn,
                DateAddedColumn,
                UriColumn,
                MimeTypeColumn
            );
        }
        
        private Column indicator_column 
            = new Column (null, "indicator", new ColumnCellStatusIndicator (null), 0.05, true, 30, 30);
        public Column IndicatorColumn {
            get { return indicator_column; }
        }
        
        private SortableColumn track_column = new SortableColumn (Catalog.GetString ("Track"), 
            new ColumnCellTrackNumber ("TrackNumber", true), 0.10, "Track", true);
        public SortableColumn TrackColumn {
            get { return track_column; }
        }
        
        private SortableColumn title_column = new SortableColumn (Catalog.GetString ("Title"), 
            new ColumnCellText ("TrackTitle", true), 0.25, "Title", true);
        public SortableColumn TitleColumn {
            get { return title_column; }
        }
        
        private SortableColumn artist_column = new SortableColumn (Catalog.GetString ("Artist"), 
            new ColumnCellText ("ArtistName", true), 0.225, "Artist", true);
        public SortableColumn ArtistColumn {
            get { return artist_column; }
        }
        
        private SortableColumn album_column = new SortableColumn (Catalog.GetString ("Album"), 
            new ColumnCellText ("AlbumTitle", true), 0.225, "Album", true);
        public SortableColumn AlbumColumn {
            get { return album_column; }
        }
        
        private SortableColumn duration_column = new SortableColumn (Catalog.GetString ("Duration"),
            new ColumnCellDuration ("Duration", true), 0.10, "Duration", true);
        public SortableColumn DurationColumn {
            get { return duration_column; }
        }
        
        private SortableColumn genre_column = new SortableColumn (Catalog.GetString ("Genre"), 
            new ColumnCellText ("Genre", true), 0.25, "Genre", false);
        public SortableColumn GenreColumn {
            get { return genre_column; }
        }
        
        private SortableColumn year_column = new SortableColumn (Catalog.GetString ("Year"), 
            new ColumnCellPositiveInt ("Year", true), 0.15, "Year", false);
        public SortableColumn YearColumn {
            get { return year_column; }
        }

        private SortableColumn file_size_column = new SortableColumn (Catalog.GetString ("File Size"), 
            new ColumnCellFileSize ("FileSize", true), 0.15, "FileSize", false);
        public SortableColumn FileSizeColumn {
            get { return file_size_column; }
        }
        
        private SortableColumn composer_column = new SortableColumn (Catalog.GetString ("Composer"), 
            new ColumnCellText ("Composer", true), 0.25, "Composer", false);
        public SortableColumn ComposerColumn {
            get { return composer_column; }
        }
        
        private SortableColumn comment_column = new SortableColumn (Catalog.GetString ("Comment"), 
            new ColumnCellText ("Comment", true), 0.25, "Comment", false);
        public SortableColumn CommentColumn {
            get { return comment_column; }
        }
        
        private SortableColumn play_count_column = new SortableColumn (Catalog.GetString ("Play Count"), 
            new ColumnCellText ("PlayCount", true), 0.15, "PlayCount", false);
        public SortableColumn PlayCountColumn {
            get { return play_count_column; }
        }
        
        private SortableColumn skip_count_column = new SortableColumn (Catalog.GetString ("Skip Count"), 
            new ColumnCellText ("SkipCount", true), 0.15, "SkipCount", false);
        public SortableColumn SkipCountColumn {
            get { return skip_count_column; }
        }
        
        private SortableColumn disc_column = new SortableColumn (Catalog.GetString ("Disc"), 
            new ColumnCellPositiveInt ("Disc", true), 0.10, "Disc", false);
        public SortableColumn DiscColumn {
            get { return disc_column; }
        }
        
        private SortableColumn rating_column = new SortableColumn (Catalog.GetString ("Rating"),
            new ColumnCellRating ("Rating", true), 0.15, "Rating", false);
        public SortableColumn RatingColumn {
            get { return rating_column; }
        }
        
        private SortableColumn last_played_column = new SortableColumn (Catalog.GetString ("Last Played"), 
            new ColumnCellDateTime ("LastPlayed", true), 0.15, "LastPlayedStamp", false);
        public SortableColumn LastPlayedColumn {
            get { return last_played_column; }
        }
        
        private SortableColumn last_skipped_column = new SortableColumn (Catalog.GetString ("Last Skipped"), 
            new ColumnCellDateTime ("LastSkipped", true), 0.15, "LastSkippedStamp", false);
        public SortableColumn LastSkippedColumn {
            get { return last_skipped_column; }
        }
        
        private SortableColumn date_added_column = new SortableColumn (Catalog.GetString ("Date Added"), 
            new ColumnCellDateTime ("DateAdded", true), 0.15, "DateAddedStamp", false);
        public SortableColumn DateAddedColumn {
            get { return date_added_column; }
        }
        
        private SortableColumn uri_column = new SortableColumn (Catalog.GetString ("Location"), 
            new ColumnCellText ("Uri", true), 0.15, "Uri", false);
        public SortableColumn UriColumn {
            get { return uri_column; }
        }
        
        private SortableColumn mime_type_column = new SortableColumn (Catalog.GetString ("Mime Type"), 
            new ColumnCellText ("MimeType", true), 0.15, "MimeType", false);
        public SortableColumn MimeTypeColumn {
            get { return mime_type_column; }
        }
    }
}
