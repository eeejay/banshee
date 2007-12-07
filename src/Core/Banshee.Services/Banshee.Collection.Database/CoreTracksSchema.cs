//
// CoreTracksSchema.cs
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
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{
    public static class CoreTracksSchema
    {
        [AttributeUsage (AttributeTargets.Field)] private class JoinColumnAttribute : Attribute { }
        
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
            [JoinColumn] Artist,
            [JoinColumn] AlbumTitle
        }
    
        private static Column [] column_values;
        private static Column [] column_values_virtual;
        
        private static MethodInfo binder_method_info = null;
        
        private static BansheeDbCommand insert_command = null;
        private static BansheeDbCommand update_command = null;
        
        static CoreTracksSchema ()
        {
            List<Column> column_values_l = new List<Column> ();
            List<Column> column_values_virtual_l = new List<Column> ();
            
            foreach (Column column in Enum.GetValues (typeof (Column))) {
                bool join = false;
                FieldInfo field = typeof (Column).GetField (column.ToString ());
                
                foreach (Attribute attr in field.GetCustomAttributes (false)) {
                    if (attr is JoinColumnAttribute) {
                        join = true;
                        break;
                    }
                }
                
                if (!join) {
                    column_values_l.Add (column);
                }
                
                column_values_virtual_l.Add (column);
            }
            
            column_values = column_values_l.ToArray ();
            column_values_virtual = column_values_virtual_l.ToArray ();

            // Generate INSERT command
            StringBuilder cols = new StringBuilder ();
            StringBuilder vals = new StringBuilder ();
            for (int i = 0, n = ColumnCount; i < n; i++) {
                cols.AppendFormat ("{0}{1}", column_values[i], (i < n - 1) ? ", " : String.Empty);
                vals.Append (i < n - 1 ? "?, " : "?");
            }

            insert_command = new BansheeDbCommand (
                String.Format (
                    "INSERT INTO CoreTracks ({0}) VALUES ({1})",
                    cols.ToString (), vals.ToString ()
                ), ColumnCount
            );

            // Generate UPDATE command
            StringBuilder set = new StringBuilder ();
            for (int i = 0, n = ColumnCount; i < n; i++) {
                set.AppendFormat ("{0} = ?{1}", column_values[i],
                        (i < n - 1) ? ", " : String.Empty);
            }

            update_command = new BansheeDbCommand (
                String.Format (
                    "UPDATE CoreTracks SET {0} WHERE TrackID = :TrackID",
                    set.ToString ()
                ), ColumnCount
            );
        }
        
        public static void Commit (LibraryTrackInfo track)
        {
            if (track.DbId < 0) {
                InsertCommit (track);
            } else {
                UpdateCommit (track);
            }
        }
        
        private static void InsertCommit (LibraryTrackInfo track)
        {
            insert_command.ApplyValues (
                null, // TrackID
                -1, // ArtistID
                -1, // AlbumID
                -1, // TagSetID
                null, // MusicBrainzID
                track.Uri == null ? null : track.Uri.AbsoluteUri, // RelativeUri
                track.MimeType, // MimeType
                track.TrackTitle, // Title
                track.TrackNumber, // TrackNumber
                track.TrackCount, // TrackCount
                track.Duration.TotalMilliseconds, // Duration
                track.Year, // Year
                track.Rating, // Rating
                track.PlayCount, // PlayCount
                track.SkipCount, // SkipCount
                DateTimeUtil.FromDateTime (track.LastPlayed), // LastPlayedStamp
                DateTimeUtil.FromDateTime (track.DateAdded) // DateAddedStamp
            );
            
            track.DbId = ServiceManager.DbConnection.Execute (insert_command);
        }
        
        private static void UpdateCommit (LibraryTrackInfo track)
        {
            /*update_command.ApplyValues (
                track.DbId, // TrackID
                -1, // ArtistID
                -1, // AlbumID
                -1, // TagSetID
                null, // MusicBrainzID
                track.Uri == null ? null : track.Uri.AbsoluteUri, // RelativeUri
                track.MimeType, // MimeType
                track.TrackTitle, // Title
                track.TrackNumber, // TrackNumber
                track.TrackCount, // TrackCount
                track.Duration.TotalMilliseconds, // Duration
                track.Year, // Year
                track.Rating, // Rating
                track.PlayCount, // PlayCount
                track.SkipCount, // SkipCount
                DateTimeUtil.FromDateTime (track.LastPlayed), // LastPlayedStamp
                DateTimeUtil.FromDateTime (track.DateAdded) // DateAddedStamp
            );*/
        }
        
        public static int ColumnCount {
            get { return column_values.Length; }
        }
        
        public static int ColumnCountVirtual {
            get { return column_values_virtual.Length; }
        }
        
        public static IEnumerable<Column> Columns {
            get {
                foreach (Column column in column_values) {
                    yield return column;
                }
            }
        }
        
        public static IEnumerable<Column> ColumnsVirtual {
            get {
                foreach (Column column in column_values_virtual) {
                    yield return column;
                }
            }
        }
    }
}
