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
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{    
    public class LibraryTrackInfo : TrackInfo
    {
        private int dbid;

        private enum Column : int {
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
            
            Artist,
            AlbumTitle
        }

        public LibraryTrackInfo(IDataReader reader) : base()
        {
            LoadFromReader(reader);
        }

        private void LoadFromReader(IDataReader reader)
        {
            dbid = ReaderGetInt32(reader, Column.TrackID);
            
            Uri = new SafeUri(ReaderGetString(reader, Column.RelativeUri));
            
            ArtistName = ReaderGetString(reader, Column.Artist);
            AlbumTitle = ReaderGetString(reader, Column.AlbumTitle);
            TrackTitle = ReaderGetString(reader, Column.Title);

            TrackNumber = ReaderGetInt32(reader, Column.TrackNumber);
            TrackCount = ReaderGetInt32(reader, Column.TrackCount);
            Year = ReaderGetInt32(reader, Column.Year);
            Rating = ReaderGetInt32(reader, Column.Rating);

            Duration = ReaderGetTimeSpan(reader, Column.Duration);
        }

        private string ReaderGetString(IDataReader reader, Column column)
        {
            int column_id = (int)column;
            return !reader.IsDBNull(column_id) 
                ? String.Intern(reader.GetString(column_id)) 
                : null;
        }

        private int ReaderGetInt32(IDataReader reader, Column column)
        {
            return reader.GetInt32((int)column);
        }

        private TimeSpan ReaderGetTimeSpan(IDataReader reader, Column column)
        {
            long raw = reader.GetInt64((int)column);
            return new TimeSpan(raw * TimeSpan.TicksPerSecond);
        }

        public int DbId {
            get { return dbid; }
        }
    }
}
