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

using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{
    public class LibraryTrackInfo : TrackInfo, IDatabaseItem
    {
        private static BansheeModelProvider<LibraryTrackInfo> provider = new BansheeModelProvider<LibraryTrackInfo> (
            "CoreTracks", ServiceManager.DbConnection
        );

        public static BansheeModelProvider<LibraryTrackInfo> Provider {
            get { return provider; }
        }

        private enum UriType : int {
            AbsolutePath,
            RelativePath,
            AbsoluteUri
        }
        
        private int dbid;
        
        public LibraryTrackInfo () : base ()
        {
            Attributes |= TrackAttributes.CanPlay;
        }

        public LibraryTrackInfo (int index) : base ()
        {
            Attributes |= TrackAttributes.CanPlay;
            DbIndex = index;
        }

        public override void Save ()
        {
            if (DbId < 0) {
                DbId = Provider.Insert (this);
            } else {
                Provider.Update (this);
            }
        }
        
        [DatabaseColumn("TrackID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public int DbId {
            get { return dbid; }
            internal set { dbid = value; }
        }
        
        private int db_index;
        public int DbIndex {
            get { return db_index; }
            set { db_index = value; }
        }

        [DatabaseColumn("ArtistID", Index = "CoreTracksArtistIndex")]
        private int artist_id;
        public int ArtistId {
            get { return artist_id; }
            set { artist_id = value; }
        }

        [DatabaseColumn("AlbumID", Index = "CoreTracksAlbumIndex")]
        private int album_id;
        public int AlbumId {
            get { return album_id; }
            set { album_id = value; }
        }

        [DatabaseVirtualColumn("Name", "CoreArtists", "ArtistID", "ArtistID")]
        public override string ArtistName {
            get { return base.ArtistName; }
            set { base.ArtistName = value; }
        }
        
        [DatabaseVirtualColumn("Title", "CoreAlbums", "AlbumID", "AlbumID")]
        public override string AlbumTitle {
            get { return base.AlbumTitle; }
            set { base.AlbumTitle = value; }
        }
        
        [DatabaseColumn]
        private int TagSetID;
        
        [DatabaseColumn]
        private string MusicBrainzID;
        
        private string uri_field;
        [DatabaseColumn("Uri")]
        private string uri {
            get {
                return
                    uri_field ??
                    (Uri == null
                        ? null
                        : (Paths.MakePathRelativeToLibrary (Uri.AbsolutePath) ?? Uri.AbsoluteUri)
                    );
            }
            set {
                uri_field = value;
                if (uri_type_field.HasValue) {
                    SetUpUri ();
                }
            }
        }
        
        private int? uri_type_field;
        [DatabaseColumn("UriType")]
        private int uri_type {
            get {
                return uri_type_field.HasValue
                    ? uri_type_field.Value
                    : (Uri == null
                        ? (int)UriType.RelativePath
                        : (Paths.MakePathRelativeToLibrary (Uri.AbsolutePath) != null
                            ? (int)UriType.RelativePath
                            : (int)UriType.AbsoluteUri));
            }
            set {
                uri_type_field = value;
                if (uri_field != null) {
                    SetUpUri ();
                }
            }
        }
        
        bool set_up;
        private void SetUpUri ()
        {
            if (set_up) {
                return;
            }
            set_up = true;
            
            if (uri_type_field.Value == (int)UriType.RelativePath) {
                uri_field = System.IO.Path.Combine (Paths.LibraryLocation, uri_field);
            }
            Uri = new SafeUri (uri_field);
        }
        
        [DatabaseColumn]
        public override string MimeType {
            get { return base.MimeType; }
            set { base.MimeType = value; }
        }
        
        [DatabaseColumn]
        public override long FileSize {
            get { return base.FileSize; }
            set { base.FileSize = value; }
        }
        
        [DatabaseColumn("Title")]
        public override string TrackTitle {
            get { return base.TrackTitle; }
            set { base.TrackTitle = value; }
        }
        
        [DatabaseColumn]
        public override int TrackNumber {
            get { return base.TrackNumber; }
            set { base.TrackNumber = value; }
        }
        
        [DatabaseColumn]
        public override int TrackCount {
            get { return base.TrackCount; }
            set { base.TrackCount = value; }
        }
        
        [DatabaseColumn]
        public override int DiscNumber {
            get { return base.DiscNumber; }
            set { base.DiscNumber = value; }
        }
        
        [DatabaseColumn("Duration")]
        private long duration {
            get { return (long)Duration.TotalMilliseconds; }
            set { Duration = new TimeSpan (value * TimeSpan.TicksPerMillisecond); }
        }
        
        [DatabaseColumn]
        public override int Year {
            get { return base.Year; }
            set { base.Year = value; }
        }
        
        [DatabaseColumn]
        public override int Rating {
            get { return base.Rating; }
            set { base.Rating = value; }
        }
        
        [DatabaseColumn]
        public override int PlayCount {
            get { return base.PlayCount; }
            set { base.PlayCount = value; }
        }
        
        [DatabaseColumn]
        public override int SkipCount {
            get { return base.SkipCount; }
            set { base.SkipCount = value; }
        }
        
        [DatabaseColumn]
        private long LastPlayedStamp {
            get { return DateTimeUtil.FromDateTime(LastPlayed); }
            set { LastPlayed = DateTimeUtil.ToDateTime(value); }
        }
        
        [DatabaseColumn]
        private long DateAddedStamp {
            get { return DateTimeUtil.FromDateTime(DateAdded); }
            set { DateAdded = DateTimeUtil.ToDateTime(value); }
        }

        private static BansheeDbCommand check_command = new BansheeDbCommand (
            "SELECT COUNT(*) FROM CoreTracks WHERE Uri = ? OR Uri = ?", 2);
        
        public static bool ContainsUri (SafeUri uri)
        {
            string relative_path = Paths.MakePathRelativeToLibrary (uri.AbsolutePath) ?? uri.AbsoluteUri;
            return Convert.ToInt32 (ServiceManager.DbConnection.ExecuteScalar (
                check_command.ApplyValues (relative_path, uri.AbsoluteUri))) > 0;
        }
    }
}
