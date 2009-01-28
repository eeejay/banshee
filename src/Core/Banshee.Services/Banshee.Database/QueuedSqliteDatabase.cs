//
// QueuedSqliteDatabase.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Threading;
using System.Collections.Generic;
using Mono.Data.Sqlite;

namespace Banshee.Database
{
    public class QueuedSqliteDatabase : IDisposable
    {
        private Queue<QueuedSqliteCommand> command_queue = new Queue<QueuedSqliteCommand>();
        private SqliteConnection connection;
        private Thread queue_thread;
        private bool dispose_requested = false;
        private bool processing_queue = false;
        private string dbpath;
        private bool connected;
        
        public QueuedSqliteDatabase(string dbpath)
        {
            this.dbpath = dbpath;
            queue_thread = new Thread(ProcessQueue);
            queue_thread.IsBackground = true;
            queue_thread.Start();
        }
        
        public void Dispose()
        {
            dispose_requested = true;
            WakeUp();
            while(processing_queue);
        }
        
        private void WaitForConnection()
        {
            while(!connected);
        }

        private void QueueCommand(QueuedSqliteCommand command)
        {
            lock(command_queue) {
                command_queue.Enqueue(command);
            }
            WakeUp();
        }
        
        public SqliteDataReader ExecuteReader(DbCommand command)
        {
            WaitForConnection();
            command.Connection = connection;
            command.CommandType = Banshee.Database.CommandType.Reader;
            QueueCommand(command);
            return command.WaitForResult() as SqliteDataReader;
        }
        
        public SqliteDataReader ExecuteReader(object command)
        {
            return ExecuteReader(new DbCommand(command.ToString()));
        }
        
        public object ExecuteScalar(DbCommand command)
        {
            WaitForConnection();
            command.Connection = connection;
            command.CommandType = Banshee.Database.CommandType.Scalar;
            QueueCommand(command);
            return command.WaitForResult();
        }

        public Int32 QueryInt32 (object command)
        {
            return Convert.ToInt32 (ExecuteScalar (command));
        }
                
        public object ExecuteScalar(object command)
        {
            return ExecuteScalar(new DbCommand(command.ToString()));
        }

        public object ExecuteScalar(string command, params object [] parameters) 
        {
            return ExecuteScalar(new DbCommand(command, parameters));
        }
        
        public int Execute(DbCommand command)
        {
            WaitForConnection();
            command.Connection = connection;
            command.CommandType = Banshee.Database.CommandType.Execute;;
            QueueCommand(command);
            command.WaitForResult();
            return command.InsertID;
        }
        
        public int Execute(object command)
        {
            return Execute(new DbCommand(command.ToString()));
        }

        public int Execute(string command, params object [] parameters) 
        {
            return Execute(new DbCommand(command, parameters));
        }
        
        public bool TableExists(string table)
        {
            return Convert.ToInt32(ExecuteScalar(@"
                SELECT COUNT(*) 
                    FROM sqlite_master
                    WHERE Type='table' AND Name=:table_name", 
                    "table_name", table)) > 0;
        }

        private void WakeUp()
        {
            Monitor.Enter(command_queue);
            Monitor.Pulse(command_queue);
            Monitor.Exit(command_queue);
        }
        
        private void ProcessQueue()
        {         
            if(connection == null) {
                connection = new SqliteConnection("Version=3,URI=file:" + dbpath);
                connection.Open();
                connected = true;
            }
            
            processing_queue = true;
            bool in_dispose_transaction = false;
            
            while(true) {
                while(command_queue.Count > 0) {
                    if(dispose_requested && !in_dispose_transaction) {
                        (new SqliteCommand("BEGIN", connection)).ExecuteNonQuery();
                        in_dispose_transaction = true;
                    }
                    
                    QueuedSqliteCommand command;
                    lock(command_queue) {
                        command = command_queue.Dequeue();
                    }
                    command.Execute();
                }

                if(dispose_requested) {
                    if(in_dispose_transaction) {
                        (new SqliteCommand("COMMIT", connection)).ExecuteNonQuery();
                    }
                    connection.Close();
                    processing_queue = false;
                    return;
                }
                
                Monitor.Enter(command_queue);
                Monitor.Wait(command_queue);
                Monitor.Exit(command_queue);
            }
        }
    }

    public enum CommandType {
        Reader,
        Scalar,
        Execute
    }

    public class QueuedSqliteCommand : SqliteCommand
    {
        private CommandType command_type;
        private object result;
        private int insert_id;
        private Exception execution_exception;
        private bool finished = false;
        
        public QueuedSqliteCommand(string command) : base(command)
        {
        }
        
        public QueuedSqliteCommand(SqliteConnection connection, string command, CommandType commandType) 
            : base(command, connection)
        {
            this.command_type = commandType;
        }
        
        public void Execute()
        {
            if(result != null) {
                throw new ApplicationException("Command has alread been executed");
            }
        
            try {
                switch(command_type) {
                    case Banshee.Database.CommandType.Reader:
                        result = ExecuteReader();
                        break;
                    case Banshee.Database.CommandType.Scalar:
                        result = ExecuteScalar();
                        break;
                    case Banshee.Database.CommandType.Execute:
                    default:
                        result = ExecuteNonQuery();
                        insert_id = LastInsertRowID();
                        break;
                }
            } catch(Exception e) {
                execution_exception = e;
            }
            
            finished = true;
        }
        
        public object WaitForResult()
        {
            while(!finished);
            
            if(execution_exception != null) {
                throw execution_exception;
            }
            
            return result;
        }
        
        public object Result {
            get { return result; }
            internal set { result = value; }
        }
        
        public int InsertID {
            get { return insert_id; }
        }
        
        public new CommandType CommandType {
            get { return command_type; }
            set { command_type = value; }
        }
    }
    
    public class DbCommand : QueuedSqliteCommand
    {
        public DbCommand(string command) : base(command)
        {
        }
                
        public DbCommand(string command, params object [] parameters) : this(command)
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
        
        public void AddParameter<T>(string name, T value)
        {
            AddParameter<T>(new DbParameter<T>(name), value);
        }
        
        public void AddParameter<T>(DbParameter<T> param, T value)
        {
            param.Value = value;
            Parameters.Add(param);
        }
    }
    
    public class DbParameter<T> : SqliteParameter
    {
        public DbParameter(string name) : base()
        {
            ParameterName = name;
        }
        
        public new T Value {
            get { return (T)base.Value; }
            set { base.Value = value; }
        }
    }
}
