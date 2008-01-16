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
using System.Data;
using Mono.Data.Sqlite;

namespace Hyena.Data.Sqlite
{
    public abstract class HyenaSqliteConnection : IDisposable
    {
        private SqliteConnection connection;

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
            IDbCommand command = connection.CreateCommand ();
            command.CommandText = @"
                SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE Type='table' AND Name=:table_name";
            
            IDbDataParameter table_param = command.CreateParameter ();
            table_param.ParameterName = "table_name";
            table_param.Value = tableName;
            
            command.Parameters.Add (table_param);
            
            try {
                return Convert.ToInt32 (command.ExecuteScalar ()) > 0;
            } catch (Exception e) {
                Console.WriteLine ("Caught exception trying to execute {0}", command.CommandText);
                throw e;
            }
        }

        public IDataReader ExecuteReader (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;

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

        public Int32 QueryInt32 (object command)
        {
            return Convert.ToInt32 (ExecuteScalar (command));
        }

        public int Execute (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;
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

#endregion

        public abstract string DatabaseFile { get; }

        public IDbConnection Connection {
            get { return connection; }
        }
    }
}
