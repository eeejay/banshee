//
// HyenaSqliteConnection.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Threading;
using System.Collections.Generic;

// NOTE: Mono.Data.Sqlite has serious threading issues.  You cannot access
//       its results from any thread but the one the SqliteConnection belongs to.
//       That is why we still use Mono.Data.SqliteClient.
using Mono.Data.SqliteClient;

namespace Hyena.Data.Sqlite
{
    public class ExecutingEventArgs : EventArgs
    {
        public readonly SqliteCommand Command;
        public ExecutingEventArgs (SqliteCommand command)
        {
            Command = command;
        }
    }
    
    public enum HyenaCommandType {
        Reader,
        Scalar,
        Execute,
    }

    public class HyenaSqliteConnection : IDisposable
    {
        private SqliteConnection connection;
        private string dbpath;

        private Queue<HyenaSqliteCommand> command_queue = new Queue<HyenaSqliteCommand>();
        private Thread queue_thread;
        private volatile bool dispose_requested = false;
        private volatile int results_ready = 0;
        private AutoResetEvent queue_signal = new AutoResetEvent (false);
        private ManualResetEvent result_ready_signal = new ManualResetEvent (false);

        private volatile Thread transaction_thread = null;
        private ManualResetEvent transaction_signal = new ManualResetEvent (true);

        internal ManualResetEvent ResultReadySignal {
            get { return result_ready_signal; }
        }
        
        public event EventHandler<ExecutingEventArgs> Executing;
        
        public HyenaSqliteConnection(string dbpath)
        {
            this.dbpath = dbpath;
            queue_thread = new Thread(ProcessQueue);
            queue_thread.IsBackground = true;
            queue_thread.Start();
        }

#region Public Query Methods
        
        // SELECT multiple column queries
        public IDataReader Query (HyenaSqliteCommand command)
        {
            lock (command) {
                command.CommandType = HyenaCommandType.Reader;
                QueueCommand(command);
                return command.WaitForResult (this) as SqliteDataReader;
            }
        }

        public IDataReader Query (HyenaSqliteCommand command, params object [] param_values)
        {
            return Query (command.ApplyValues (param_values));
        }

        public IDataReader Query (string command_str, params object [] param_values)
        {
            return Query (new HyenaSqliteCommand (command_str, param_values));
        }
        
        public IDataReader Query (object command)
        {
            return Query (new HyenaSqliteCommand (command.ToString ()));
        }
        
        // SELECT single column queries
        public T Query<T> (HyenaSqliteCommand command)
        {
            object result = null;
            lock (command) {
                command.CommandType = HyenaCommandType.Scalar;
                QueueCommand(command);
                result = command.WaitForResult (this);
            }

            return (T)SqliteUtils.FromDbFormat (typeof (T), result);
        }

        public T Query<T> (HyenaSqliteCommand command, params object [] param_values)
        {
            return Query<T> (command.ApplyValues (param_values));
        }

        public T Query<T> (string command_str, params object [] param_values)
        {
            return Query<T> (new HyenaSqliteCommand (command_str, param_values));
        }

        public T Query<T> (object command)
        {
            return Query<T> (new HyenaSqliteCommand (command.ToString ()));
        }

        // INSERT, UPDATE, DELETE queries
        public int Execute (HyenaSqliteCommand command)
        {
            lock (command) {
                command.CommandType = HyenaCommandType.Execute;;
                QueueCommand(command);
                return (int) command.WaitForResult (this);
            }
        }

        public int Execute (HyenaSqliteCommand command, params object [] param_values)
        {
            return Execute (command.ApplyValues (param_values));
        }

        public int Execute (string command_str, params object [] param_values)
        {
            return Execute (new HyenaSqliteCommand (command_str, param_values));
        }

        public int Execute (object command)
        {
            return Execute (new HyenaSqliteCommand (command.ToString ()));
        }

#endregion

#region Public Utility Methods
        
        public void BeginTransaction ()
        {
            if (transaction_thread == Thread.CurrentThread) {
                throw new Exception ("Can't start a recursive transaction");
            }

            while (transaction_thread != Thread.CurrentThread) {
                if (transaction_thread != null) {
                    // Wait for the existing transaction to finish before this thread proceeds
                    transaction_signal.WaitOne ();
                }

                lock (command_queue) {
                    if (transaction_thread == null) {
                        transaction_thread = Thread.CurrentThread;
                        transaction_signal.Reset ();
                    }
                }
            }

            Execute ("BEGIN TRANSACTION");
        }

        public void CommitTransaction ()
        {
            if (transaction_thread != Thread.CurrentThread) {
                throw new Exception ("Can't commit from outside a transaction");
            }

            Execute ("COMMIT TRANSACTION");

            lock (command_queue) {
                transaction_thread = null;
                // Let any other threads continue
                transaction_signal.Set (); 
            }
        }

        public void RollbackTransaction ()
        {
            if (transaction_thread != Thread.CurrentThread) {
                throw new Exception ("Can't rollback from outside a transaction");
            }

            Execute ("ROLLBACK");

            lock (command_queue) {
                transaction_thread = null;
            
                // Let any other threads continue
                transaction_signal.Set (); 
            }
        }

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
        public IDictionary<string, string> GetSchema (string table_name)
        {
            Dictionary<string, string> schema = new Dictionary<string,string> ();
            SchemaClosure (table_name, delegate (string column) {
                schema.Add (column, null);
            });
            return schema;
        }

#endregion

#region Private Queue Methods

        private void QueueCommand(HyenaSqliteCommand command)
        {
            while (true) {
                lock (command_queue) {
                    if (transaction_thread == null || Thread.CurrentThread == transaction_thread) {
                        command_queue.Enqueue (command);
                        break;
                    }
                }

                transaction_signal.WaitOne ();
            }
            queue_signal.Set ();
        }

        internal void ClaimResult ()
        {
            lock (command_queue) {
                results_ready++;
                if (results_ready == 0) {
                    result_ready_signal.Reset ();
                }
            }
        }

        private void ProcessQueue()
        {         
            if (connection == null) {
                connection = new SqliteConnection (String.Format ("Version=3,URI=file:{0}", dbpath));
                connection.Open ();
            }
            
            // Keep handling queries
            while (!dispose_requested) {
                while (command_queue.Count > 0) {
                    HyenaSqliteCommand command;
                    lock (command_queue) {
                        command = command_queue.Dequeue ();
                    }
                    
                    command.Execute (this, connection);

                    lock (command_queue) {
                        results_ready++;
                        result_ready_signal.Set ();
                    }
                }

                queue_signal.WaitOne ();
            }

            // Finish
            connection.Close ();
        }
        
        internal void OnExecuting (SqliteCommand command)
        {
            EventHandler<ExecutingEventArgs> handler = Executing;
            if (handler != null) {
                handler (this, new ExecutingEventArgs (command));
            }
        }

#endregion

        public void Dispose()
        {
            dispose_requested = true;
            queue_signal.Set ();
            queue_thread.Join ();
        }
    }
}
