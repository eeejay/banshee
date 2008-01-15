//
// BansheeDbConnection.cs
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

namespace Hyena.Data
{
    public abstract class HyenaDbConnection : IDisposable
    {
        private SqliteConnection connection;

        public HyenaDbConnection() : this(true)
        {
        }

        public HyenaDbConnection(bool connect)
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

        public IDataReader ExecuteReader (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;
            return command.ExecuteReader ();
        }

        public IDataReader ExecuteReader (HyenaDbCommand command)
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
            return command.ExecuteScalar ();
        }

        public object ExecuteScalar (HyenaDbCommand command)
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
            command.ExecuteNonQuery ();
            return command.LastInsertRowID ();
        }

        public int Execute (HyenaDbCommand command)
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
    
    public class HyenaDbCommand
    {
        private SqliteCommand command;

#region Properties

        public SqliteCommand Command {
            get { return command; }
        }

        public SqliteParameterCollection Parameters {
            get { return command.Parameters; }
        }

        public string CommandText {
            get { return command.CommandText; }
        }

#endregion

        public HyenaDbCommand(string command)
        {
            this.command = new SqliteCommand (command);
        }

        public HyenaDbCommand (string command, int num_params) : this (command)
        {
            for (int i = 0; i < num_params; i++) {
                Parameters.Add (new SqliteParameter ());
            }
        }

        public HyenaDbCommand (string command, params object [] param_values) : this (command, param_values.Length)
        {
            ApplyValues (param_values);
        }

        public HyenaDbCommand ApplyValues (params object [] param_values)
        {
            if (param_values.Length != Parameters.Count) {
                throw new ArgumentException (String.Format (
                    "Command has {0} parameters, but {1} values given.", Parameters.Count, param_values.Length
                ));
            }

            for (int i = 0; i < param_values.Length; i++) {
                Parameters[i].Value = param_values[i];
            }

            return this;
        }
        
        public void AddNamedParameter (string name, object value)
        {
            SqliteParameter param = new SqliteParameter (name, DbType.String);
            param.Value = value;
            Parameters.Add (param);
        }
                
        /*public DbCommand(string command, params object [] parameters) : this(command)
        {
            for(int i = 0; i < parameters.Length;) {
                SqliteParameter param;
                
                if(parameters[i] is SqliteParameter) {
                    param = (SqliteParameter)parameters[i];
                    if(i < parameters.Length - 1 && !(parameters[i + 1] is SqliteParameter)) {
                        param.Value = parameters[i + 1];
                        i += 2;
                    } else {
                        i++;
                    }
                } else {
                    param = new SqliteParameter();
                    param.ParameterName = (string)parameters[i];
                    param.Value = parameters[i + 1];
                    i += 2;
                }
                
                Parameters.Add(param);
            }
        }
        
        public void AddParameter (object value)
        {
            SqliteParameter param = new SqliteParameter ();
            param.Value = value;
            Parameters.Add (param);
        }
        
        public void AddParameter<T>(string name, T value)
        {
            AddParameter<T>(new DbParameter<T>(name), value);
        }
        
        public void AddParameter<T>(DbParameter<T> param, T value)
        {
            param.Value = value;
            Parameters.Add(param);
        }*/
    }
}