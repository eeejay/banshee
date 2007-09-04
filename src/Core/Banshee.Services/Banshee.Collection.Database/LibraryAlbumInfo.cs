//
// LibraryAlbumInfo.cs
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

namespace Banshee.Collection.Database
{    
    public class LibraryAlbumInfo : AlbumInfo
    {
        private int dbid;

        private enum Column : int {
            AlbumID,
            Title,
            ArtistName
        }

        public LibraryAlbumInfo(IDataReader reader) : base(null)
        {
            LoadFromReader(reader);
        }

        private void LoadFromReader(IDataReader reader)
        {
            dbid = Convert.ToInt32(reader[(int)Column.AlbumID]);
            Title = (string)reader[(int)Column.Title];
            ArtistName = (string)reader[(int)Column.ArtistName];
        }

        public int DbId {
            get { return dbid; }
        }
    }
}
