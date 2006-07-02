/***************************************************************************
 *  QueuedSqliteDatabase.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Threading;
using System.Collections.Generic;
using Mono.Data.SqliteClient;

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
                Monitor.Enter(command_queue);
                Monitor.Pulse(command_queue);
                Monitor.Exit(command_queue);
            }
        }
        
        public SqliteDataReader Query(object query)
        {
            WaitForConnection();
            QueuedSqliteCommand command = new QueuedSqliteCommand(connection, 
                query.ToString(), Banshee.Database.CommandType.Reader);
            QueueCommand(command);
            return command.WaitForResult() as SqliteDataReader;
        }
                
        public object QuerySingle(object query)
        {
            WaitForConnection();
            QueuedSqliteCommand command = new QueuedSqliteCommand(connection, 
                query.ToString(), Banshee.Database.CommandType.Scalar);
            QueueCommand(command);
            return command.WaitForResult(); 
        }
        
        public int Execute(object query)
        {
            WaitForConnection();
            QueuedSqliteCommand command = new QueuedSqliteCommand(connection, 
                query.ToString(), Banshee.Database.CommandType.Execute);
            QueueCommand(command);
            command.WaitForResult();
            return command.InsertID;
        }
        
        public bool TableExists(string table)
        {
            return Convert.ToInt32(QuerySingle(String.Format(@"
                SELECT COUNT(*) 
                    FROM sqlite_master
                    WHERE Type='table' AND Name='{0}'", 
                    table))) > 0;
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

    internal enum CommandType {
        Reader,
        Scalar,
        Execute
    }

    internal class QueuedSqliteCommand : SqliteCommand
    {
        private CommandType command_type;
        private object result;
        private int insert_id;
        private Exception execution_exception;
        private bool finished = false;
        
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
    }
}
