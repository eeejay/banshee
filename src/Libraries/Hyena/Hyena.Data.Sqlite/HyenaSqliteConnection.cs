//
// HyenaSqliteConnection.cs
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
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Mono.Data.Sqlite;

namespace Hyena.Data.Sqlite
{
    public abstract class HyenaSqliteConnection : IDisposable
    {
        private SqliteConnection connection;

        private static Thread main_thread;
        static HyenaSqliteConnection () {
            main_thread = Thread.CurrentThread;
        }

        private static bool InMainThread {
            get { return main_thread.Equals (Thread.CurrentThread); }
        }

        public HyenaSqliteConnection () : this (true)
        {
        }

        public HyenaSqliteConnection (bool connect)
        {
            if (connect) {
                Open ();
            }
        }

        public void Dispose ()
        {
            Close ();
        }

        public void Open ()
        {
            lock (this) {
                if (connection != null) {
                    return;
                }

                string dbfile = DatabaseFile;
                connection = new SqliteConnection (String.Format ("Version=3,URI=file:{0}", dbfile));
                connection.Open ();

                Execute (@"
                    PRAGMA synchronous = OFF;
                    PRAGMA cache_size = 32768;
                ");
            }
        }

        public void Close ()
        {
            lock (this) {
                if (connection != null) {
                    connection.Close ();
                    connection = null;
                }
            }
        }

#region Convenience methods 

        public bool TableExists (string tableName)
        {
            return Exists ("table", tableName);
        }
        
        public bool IndexExists (string indexName)
        {
            return Exists ("index", indexName);
        }
        
        private bool Exists (string type, string name)
        {
            return
                Query<int> (
                    String.Format (@"
                        SELECT COUNT(*)
                            FROM sqlite_master
                            WHERE Type='{0}' AND Name='{1}'",
                        type, name)
                ) > 0;
        }
        
        private delegate void SchemaHandler (string column);
        
        private void SchemaClosure (string table_name, SchemaHandler code)
        {
            string sql = Query<string> (String.Format (
                "SELECT sql FROM sqlite_master WHERE Name='{0}'", table_name));
            if (String.IsNullOrEmpty (sql)) {
                throw new Exception (String.Format (
                    "Cannot get schema for {0} because it does not exist", table_name));
            }
            sql = sql.Substring (sql.IndexOf ('(') + 1);
            foreach (string column_def in sql.Split (',')) {
                string column_def_t = column_def.Trim ();
                int ws_index = column_def_t.IndexOfAny (ws_chars);
                code (column_def_t.Substring (0, ws_index));
            }
        }
        
        public bool ColumnExists (string tableName, string columnName)
        {
            bool value = false;
            SchemaClosure (tableName, delegate (string column) {
                if (column == columnName) {
                    value = true;
                    return;
                }
            });
            return value;
        }
        
        private static readonly char [] ws_chars = new char [] { ' ', '\t', '\n', '\r' };
        public Dictionary<string, string> GetSchema (string table_name)
        {
            Dictionary<string, string> schema = new Dictionary<string,string> ();
            SchemaClosure (table_name, delegate (string column) {
                schema.Add (column, null);
            });
            return schema;
        }

        public IDataReader ExecuteReader (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;

            if (!InMainThread) {
                Console.WriteLine ("About to execute command not in main thread: {0}", command.CommandText);
            }

            try {
                return command.ExecuteReader ();
            } catch (Exception e) {
                Console.WriteLine ("Caught exception trying to execute {0}", command.CommandText);
                throw e;
            }
        }

        public IDataReader ExecuteReader (HyenaSqliteCommand command)
        {
            return ExecuteReader (command.Command);
        }

        public IDataReader ExecuteReader (object command)
        {
            return ExecuteReader (new SqliteCommand (command.ToString ()));
        }

        public object ExecuteScalar (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;

            if (!InMainThread) {
                Console.WriteLine ("About to execute command not in main thread: {0}", command.CommandText);
            }

            try {
                return command.ExecuteScalar ();
            } catch (Exception e) {
                Console.WriteLine ("Caught exception trying to execute {0}", command.CommandText);
                throw e;
            }
        }

        public object ExecuteScalar (HyenaSqliteCommand command)
        {
            return ExecuteScalar (command.Command);
        }

        public object ExecuteScalar (object command)
        {
            return ExecuteScalar (new SqliteCommand (command.ToString ()));
        }
        
        public T Query<T> (SqliteCommand command)
        {
            return (T) SqliteUtils.FromDbFormat (typeof (T),
                Convert.ChangeType (ExecuteScalar (command), typeof (T)));
        }
        
        public T Query<T> (HyenaSqliteCommand command)
        {
            return Query<T> (command.Command);
        }

        public T Query<T> (object command)
        {
            return Query<T> (new SqliteCommand (command.ToString ()));
        }

        public int Execute (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;

            if (!InMainThread) {
                Console.WriteLine ("About to execute command not in main thread: {0}", command.CommandText);
            }

            try {
                command.ExecuteNonQuery ();
            } catch (Exception e) {
                Console.WriteLine ("Caught exception trying to execute {0}", command.CommandText);
                throw e;
            }
            return command.LastInsertRowID ();
        }

        public int Execute (HyenaSqliteCommand command)
        {
            return Execute (command.Command);
        }

        public int Execute (object command)
        {
            return Execute (new SqliteCommand (command.ToString ()));
        }
        
        public int LastInsertRowId {
            get { return connection.LastInsertRowId; }
        }

#endregion

        public abstract string DatabaseFile { get; }

        public IDbConnection Connection {
            get { return connection; }
        }
    }
}
