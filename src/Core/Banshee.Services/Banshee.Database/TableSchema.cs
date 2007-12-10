//
// TableSchema.cs
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
using System.Text;
using System.Collections.Generic;

using Banshee.ServiceStack;

namespace Banshee.Database
{
    public class TableSchema
    {
        public static readonly TableSchema CoreTracks = new TableSchema ("CoreTracks");
        public static readonly TableSchema CoreAlbums = new TableSchema ("CoreAlbums");
        public static readonly TableSchema CoreArtists = new TableSchema ("CoreArtists");
    
        private string table_name;
        private string [] columns;
        
        private BansheeDbCommand insert_command;
        private BansheeDbCommand update_command;
        
        public TableSchema (string tableName)
        {
            table_name = tableName;
            
            ParseColumns ();
            GenerateInsertUpdateCommands ();
        }
        
        private void ParseColumns ()
        {
            BansheeDbCommand command = new BansheeDbCommand ("SELECT sql FROM sqlite_master WHERE name = :table_name");
            command.AddNamedParameter ("table_name", table_name);
            string schema = (string)ServiceManager.DbConnection.ExecuteScalar (command);
            
            List<string> columns_l = new List<string> ();
            
            schema = schema.Substring (schema.IndexOf ('(') + 1);
            foreach (string column_def in schema.Split (',')) {
                string column_def_t = column_def.Trim ();
                int ws_index = column_def_t.IndexOfAny (new char [] { ' ', '\t', '\n', '\r' });
                columns_l.Add (column_def_t.Substring (0, ws_index));
            }
            
            columns = columns_l.ToArray ();
        }
        
        private void GenerateInsertUpdateCommands ()
        {
            // Generate INSERT command
            StringBuilder cols = new StringBuilder ();
            StringBuilder vals = new StringBuilder ();
            for (int i = 0, n = ColumnCount; i < n; i++) {
                cols.AppendFormat ("{0}{1}", columns[i], (i < n - 1) ? ", " : String.Empty);
                vals.Append (i < n - 1 ? "?, " : "?");
            }

            insert_command = new BansheeDbCommand (
                String.Format ("INSERT INTO {0} ({1}) VALUES ({2})", TableName,
                    cols.ToString (), vals.ToString ()), 
                ColumnCount);

            // Generate UPDATE command
            StringBuilder update_set = new StringBuilder ();
            for (int i = 0, n = ColumnCount; i < n; i++) {
                update_set.AppendFormat ("{0} = ?{1}", columns[i], (i < n - 1) ? ", " : String.Empty);
            }

            update_command = new BansheeDbCommand (
                String.Format ("UPDATE {0} SET {1} WHERE TrackID = ?", TableName, update_set.ToString ()), 
                ColumnCount + 1);
        }
        
        public string TableName {
            get { return table_name; }
        }
        
        public string [] Columns {
            get { return columns; }
        }
        
        public int ColumnCount {
            get { return columns.Length; }
        }
        
        public BansheeDbCommand InsertCommand {
            get { return insert_command; }
        }
        
        public BansheeDbCommand UpdateCommand {
            get { return update_command; }
        }
    }
}
