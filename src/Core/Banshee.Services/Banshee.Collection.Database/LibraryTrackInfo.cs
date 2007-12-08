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
            dbid = ReaderGetInt32 (reader, CoreTracksSchema.Column.TrackID);
            
            Uri = new SafeUri (ReaderGetString (reader, CoreTracksSchema.Column.RelativeUri));
            
            ArtistName = ReaderGetString (reader, CoreTracksSchema.Column.Artist);
            ArtistId = ReaderGetInt32 (reader, CoreTracksSchema.Column.ArtistID);

            AlbumTitle = ReaderGetString (reader, CoreTracksSchema.Column.AlbumTitle);
            AlbumId = ReaderGetInt32 (reader, CoreTracksSchema.Column.AlbumID);

            TrackTitle = ReaderGetString (reader, CoreTracksSchema.Column.Title);

            TrackNumber = ReaderGetInt32 (reader, CoreTracksSchema.Column.TrackNumber);
            TrackCount = ReaderGetInt32 (reader, CoreTracksSchema.Column.TrackCount);
            Year = ReaderGetInt32 (reader, CoreTracksSchema.Column.Year);
            Rating = ReaderGetInt32 (reader, CoreTracksSchema.Column.Rating);

            Duration = ReaderGetTimeSpan (reader, CoreTracksSchema.Column.Duration);
            
            Attributes |= TrackAttributes.CanPlay;
        }

        private string ReaderGetString (IDataReader reader, CoreTracksSchema.Column column)
        {
            int column_id = (int) column;
            return !reader.IsDBNull (column_id) 
                ? String.Intern (reader.GetString (column_id)) 
                : null;
        }

        private int ReaderGetInt32 (IDataReader reader, CoreTracksSchema.Column column)
        {
            return reader.GetInt32 ((int) column);
        }

        private TimeSpan ReaderGetTimeSpan (IDataReader reader, CoreTracksSchema.Column column)
        {
            long raw = reader.GetInt64 ((int) column);
            return new TimeSpan (raw * TimeSpan.TicksPerSecond);
        }
        
        public override void Save ()
        {
            CoreTracksSchema.Commit (this);
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
    }
}
