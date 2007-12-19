//
// LibraryTrackInfo.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Banshee.Base;
using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{    
    public class LibraryTrackInfo : TrackInfo
    {
        public enum Column : int {
            TrackID,
            ArtistID,
            AlbumID,
            TagSetID,
            MusicBrainzID,
            RelativeUri,
            MimeType,
            Title,
            TrackNumber,
            TrackCount,
            Duration,
            Year,
            Rating,
            PlayCount,
            SkipCount,
            LastPlayedStamp,
            DateAddedStamp,
            
            // These columns are virtual - they are not actually 
            // in CoreTracks and are returned on join selects
            Artist,
            AlbumTitle
        }
        
        private int dbid;
        private int db_index;

        private int artist_id;
        private int album_id;
        
        public LibraryTrackInfo () : base ()
        {
            dbid = -1;
        }

        public LibraryTrackInfo (IDataReader reader, int index) : base ()
        {
            LoadFromReader (reader);
            DbIndex = index;
        }

        private void LoadFromReader (IDataReader reader)
        {
            dbid = ReaderGetInt32 (reader, Column.TrackID);
            
            Uri = new SafeUri (ReaderGetString (reader, Column.RelativeUri));
            
            ArtistName = ReaderGetString (reader, Column.Artist);
            ArtistId = ReaderGetInt32 (reader, Column.ArtistID);

            AlbumTitle = ReaderGetString (reader, Column.AlbumTitle);
            AlbumId = ReaderGetInt32 (reader, Column.AlbumID);

            TrackTitle = ReaderGetString (reader, Column.Title);

            TrackNumber = ReaderGetInt32 (reader, Column.TrackNumber);
            TrackCount = ReaderGetInt32 (reader, Column.TrackCount);
            Year = ReaderGetInt32 (reader, Column.Year);
            Rating = ReaderGetInt32 (reader, Column.Rating);

            Duration = ReaderGetTimeSpan (reader, Column.Duration);

            PlayCount = ReaderGetInt32 (reader, Column.PlayCount);
            SkipCount = ReaderGetInt32 (reader, Column.SkipCount);

            LastPlayed = ReaderGetDateTime (reader, Column.LastPlayedStamp);
            DateAdded = ReaderGetDateTime (reader, Column.DateAddedStamp);
            
            Attributes |= TrackAttributes.CanPlay;
        }

        private string ReaderGetString (IDataReader reader, Column column)
        {
            int column_id = (int) column;
            return !reader.IsDBNull (column_id) 
                ? String.Intern (reader.GetString (column_id)) 
                : null;
        }

        private int ReaderGetInt32 (IDataReader reader, Column column)
        {
            return reader.GetInt32 ((int) column);
        }

        private TimeSpan ReaderGetTimeSpan (IDataReader reader, Column column)
        {
            long raw = reader.GetInt64 ((int) column);
            return new TimeSpan (raw * TimeSpan.TicksPerMillisecond);
        }

        private DateTime ReaderGetDateTime (IDataReader reader, Column column)
        {
            long raw = reader.GetInt64 ((int) column);
            return DateTimeUtil.ToDateTime (raw);
        }

        public override void Save ()
        {
            if (DbId < 0) {
                InsertCommit ();
            } else {
                UpdateCommit ();
            }
        }
        
        private void InsertCommit ()
        {
            TableSchema.CoreTracks.InsertCommand.ApplyValues (
                null, // TrackID
                ArtistId,
                AlbumId,
                -1, // TagSetID
                null, // MusicBrainzID
                Uri == null ? null : Uri.AbsoluteUri, // RelativeUri
                MimeType,
                TrackTitle,
                TrackNumber,
                TrackCount,
                Duration.TotalMilliseconds,
                Year,
                Rating,
                PlayCount,
                SkipCount,
                DateTimeUtil.FromDateTime (LastPlayed),
                DateTimeUtil.FromDateTime (DateAdded)
            );
            
            DbId = ServiceManager.DbConnection.Execute (TableSchema.CoreTracks.InsertCommand);
        }
        
        private void UpdateCommit ()
        {
            TableSchema.CoreTracks.UpdateCommand.ApplyValues (
                DbId, // TrackID
                ArtistId,
                AlbumId,
                -1, // TagSetID
                null, // MusicBrainzID
                Uri == null ? null : Uri.AbsoluteUri, // RelativeUri
                MimeType,
                TrackTitle,
                TrackNumber,
                TrackCount,
                Duration.TotalMilliseconds,
                Year,
                Rating,
                PlayCount,
                SkipCount,
                DateTimeUtil.FromDateTime (LastPlayed),
                DateTimeUtil.FromDateTime (DateAdded),
                DbId // TrackID (again, for WHERE clause)
            );

            Console.WriteLine ("Updating: {0}", TableSchema.CoreTracks.UpdateCommand.CommandText);
            ServiceManager.DbConnection.Execute (TableSchema.CoreTracks.UpdateCommand);
        }

        public int DbId {
            get { return dbid; }
            set { dbid = value; }
        }
        
        public int DbIndex {
            get { return db_index; }
            internal set { db_index = value; }
        }

        public int ArtistId {
            get { return artist_id; }
            set { artist_id = value; }
        }

        public int AlbumId {
            get { return album_id; }
            set { album_id = value; }
        }

        private static BansheeDbCommand check_command = new BansheeDbCommand ("SELECT COUNT(*) FROM CoreTracks WHERE RelativeUri = ?", 1);
        public static bool ContainsPath (string path)
        {
            return Convert.ToInt32 (ServiceManager.DbConnection.ExecuteScalar (check_command.ApplyValues (path))) > 0;
        }
    }
}
