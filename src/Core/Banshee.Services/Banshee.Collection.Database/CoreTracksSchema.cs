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
        
        private static string insert_command_text = null;
        private static string update_command_text = null;
        
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
            DbCommand command = InsertCommand;
            
            command.AddParameter (-1); // TrackID
            command.AddParameter (-1); // ArtistID
            command.AddParameter (-1); // AlbumID
            command.AddParameter (-1); // TagSetID
            command.AddParameter (null); // MusicBrainzID
            command.AddParameter (track.Uri == null ? null : track.Uri.AbsoluteUri); // RelativeUri
            command.AddParameter (track.MimeType); // MimeType
            command.AddParameter (track.TrackTitle); // Title
            command.AddParameter (track.TrackNumber); // TrackNumber
            command.AddParameter (track.TrackCount); // TrackCount
            command.AddParameter (track.Duration.TotalMilliseconds); // Duration
            command.AddParameter (track.Year); // Year
            command.AddParameter (track.Rating); // Rating
            command.AddParameter (track.PlayCount); // PlayCount
            command.AddParameter (track.SkipCount); // SkipCount
            command.AddParameter (DateTimeUtil.FromDateTime (track.LastPlayed)); // LastPlayedStamp
            command.AddParameter (DateTimeUtil.FromDateTime (track.DateAdded)); // DateAddedStamp
            
            command.ExecuteNonQuery ();
        }
        
        private static void UpdateCommit (LibraryTrackInfo track)
        {
        }
        
        private static void BuildInsertCommandText ()
        {
            StringBuilder builder = new StringBuilder ();
            builder.Append ("INSERT INTO CoreTracks (");
            
            for (int i = 0, n = ColumnCount; i < n; i++) {
                builder.AppendFormat ("{0}{1}", column_values[i], i < n - 1 ? ", " : String.Empty);
            }
            
            builder.Append (") VALUES (");
            
            for (int i = 0, n = ColumnCount; i < n; i++) {
                builder.Append (i < n - 1 ? "?, " : "?");
            }
            
            builder.Append (")");
            insert_command_text = builder.ToString ();
        }
        
        private static void BuildUpdateCommandText ()
        {
            StringBuilder builder = new StringBuilder ();
            builder.Append ("UPDATE CoreTracks SET ");
            
            int i = 0;
            foreach (Column column in Columns) {
                builder.AppendFormat ("{0} = ?", column);
                if (i++ < ColumnCount - 1) {
                    builder.Append (", ");
                }
            }
            
            builder.Append (" WHERE TrackID = ? LIMIT 1");
            update_command_text = builder.ToString ();
            Console.WriteLine (update_command_text);
        }
        
        private static string InsertCommandText {
            get { 
                if (insert_command_text == null) {
                    BuildInsertCommandText ();
                }
                return insert_command_text;
            }
        }
        
        private static string UpdateCommandText {
            get { 
                if (update_command_text == null) {
                    BuildUpdateCommandText ();
                }
                return update_command_text;
            }
        }
        
        public static DbCommand InsertCommand {
            get { 
                DbCommand command = ServiceManager.DbConnection.CreateCommand ();
                command.CommandText = InsertCommandText;
                return command;
            }
        }
        
        public static DbCommand UpdateCommand {
            get { 
                DbCommand command = ServiceManager.DbConnection.CreateCommand ();
                command.CommandText = UpdateCommandText;
                return command;
            }
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
