/*************************************************************************** 
 *  DatabaseManager.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
using System.Data;
using System.Threading;
using System.Collections.Generic;

using Migo.TaskCore;

namespace Migo.Syndication.Data
{   
    // This is a crappy implementation that I wrote to bootstrap the FeedManager
    // work, it really shouldn't used and I plan to phase it out.
    public class DatabaseManager
    {
        private static bool disposed;        
        
        private static IDbConnection conn;
        private static IDbTransaction transaction;
        
        private static AsyncCommandQueue<QueuedDbCommand> commandQueue;
        
        private static readonly object sync = new object ();
        
        public static IDbConnection Connection
        {
            get {
                lock (sync) {
                    CheckDisposed ();
                    return conn;
                }
            }
            
            set {
                lock (sync) {
                    CheckDisposed ();
                    conn = value;
                }
            }            
        }

        public static object SyncRoot 
        {
            get { return sync; }
        }

        private static void BeginTransaction ()
        {
            lock (sync) {
                if (conn != null && transaction == null) {
                    transaction = conn.BeginTransaction ();
                }
            }
        }
        
        private static void CommitTransaction ()
        {
            lock (sync) {        
                if (transaction != null) {
                    transaction.Commit ();
                    transaction = null;
                }
            }
        }
/*        
        private static void RollbackTransaction ()
        {
            lock (sync) {        
                if (transaction != null) {
                    transaction.Rollback ();
                    transaction = null;
                }
            }
        }        
*/        
        public static IDbCommand CreateCommand ()
        {
            lock (sync) {   
                CheckDisposed ();
                return CreateCommandImpl (null, null);
            }
        }

        public static IDbCommand CreateCommand (string queryText)
        {
            lock (sync) {
                CheckDisposed ();
                return CreateCommandImpl (queryText, null);                        
            }
        }        

        public static IDbCommand CreateCommand (string queryText, 
                                                params string[] parameters)
        {
            lock (sync) {
                CheckDisposed ();
                return CreateCommandImpl (queryText, parameters);                        
            }
        }     

        private static IDbCommand CreateCommandImpl (string queryText, 
                                                     string[] parameters)
        {
            IDbCommand comm = null;
                
            if (String.IsNullOrEmpty (queryText) && parameters == null) {
                comm = DataUtility.CreateCommand (conn);
            } else if (!String.IsNullOrEmpty (queryText) && parameters == null) {
                comm = DataUtility.CreateCommand (conn, queryText);
            } else if (!String.IsNullOrEmpty (queryText) && parameters != null) {
                comm = DataUtility.CreateCommand (conn, queryText, parameters);
            }
            
            return comm;
        }

        public static void Dispose ()
        {
            if (SetDisposed ()) {
                commandQueue.Dispose ();
                
                commandQueue.QueueProcessing -= OnQueueProcessing;
                commandQueue.QueueProcessed -= OnQueueProcessed;
                
                commandQueue = null;
                
                conn = null;                
            }
        }        
        
        private static bool SetDisposed ()
        {
            bool ret = false;
            
            lock (sync) {
                if (!disposed) {
                    ret = disposed = true;
                }
            }
            
            return ret;
        }
        
        public static IDataReader ExecuteReader (string queryText)
        {   
            IDbCommand comm;
            QueuedDbCommand qdc;
            
            lock (sync) {
                CheckDisposed ();
                CheckQueryText (queryText);
                
                comm = DataUtility.CreateCommand (conn, queryText);
                
                qdc = ExecuteReaderImpl (comm);
            }
            
            using (qdc) {
                return qdc.ReaderResult;
            }
        }    

        public IDataReader ExecuteReader (IDbCommand comm)
        {
            if (comm == null) {
                throw new ArgumentNullException ("comm");
            }

            QueuedDbCommand qdc;
            
            lock (sync) {
                CheckDisposed ();
                qdc = ExecuteReaderImpl (comm);                
            }
            
            using (qdc) {
                return qdc.ReaderResult;
            }
        }        
        
        public IDataReader ExecuteReader (string queryText, params string[] parameters)
        {
            throw new NotImplementedException ();
        }        

        public IDataReader ExecuteReader (IDbCommand comm, params string[] parameters)
        {
            throw new NotImplementedException ();
        }

        private static QueuedDbCommand ExecuteReaderImpl (IDbCommand comm)
        {
            QueuedDbCommand queuedCommand = QueuedDbCommand.CreateReader (comm);
            commandQueue.Register (queuedCommand);
            return queuedCommand;
        }
        
        public object ExecuteScalar (string queryText)
        {
            throw new NotImplementedException ();
        }          
                 
        public static object ExecuteScalar (IDbCommand comm)
        {
            if (comm == null) {
                throw new ArgumentNullException ("comm");
            }

            QueuedDbCommand qdc;
            
            lock (sync) {
                CheckDisposed ();
                qdc = ExecuteScalarImpl (comm);                
            }
            
            using (qdc) {
                return qdc.ScalarResult;
            }
        }        
        
        public static object ExecuteScalar (string queryText, params string[] parameters)
        {
            IDbCommand comm;        
            QueuedDbCommand qdc;
            
            lock (sync) {
                CheckDisposed ();
                CheckQueryText (queryText);
                
                comm = DataUtility.CreateCommand (
                    conn, queryText, parameters
                );
                
                qdc = ExecuteScalarImpl (comm);                
            }
            
            using (qdc) {
                return qdc.ScalarResult;
            }
        }          
       
        public static object ExecuteScalar (IDbCommand comm, params string[] parameters)
        {
            throw new NotImplementedException ();
        }
        
        private static QueuedDbCommand ExecuteScalarImpl (IDbCommand comm)
        {
            QueuedDbCommand queuedCommand = QueuedDbCommand.CreateScalar (comm);
            commandQueue.Register (queuedCommand);
            return queuedCommand;
        }        
        
        public static void ExecuteNonQuery (string queryText)
        {
            IDbCommand comm;
            
            lock (sync) {  
                CheckDisposed ();
                CheckQueryText (queryText);
                comm = DataUtility.CreateCommand (conn, queryText);
                
                ExecuteNonQueryImpl (comm);
            }
        }    
        
        public static void ExecuteNonQuery (string queryText, params string[] parameters)
        {       
            IDbCommand comm;        
                    
            lock (sync) {  
                CheckDisposed ();
                CheckQueryText (queryText);
                
                comm = DataUtility.CreateCommand (
                    conn, queryText, parameters
                );
                
                ExecuteNonQueryImpl (comm);
            }
        }        
        
        public void ExecuteNonQuery (IDbCommand comm)
        {
            if (comm == null) {
                throw new ArgumentNullException ("comm");
            }
            
            lock (sync) {
                CheckDisposed ();
                ExecuteNonQueryImpl (comm);                
            }
        }
               
        public void ExecuteNonQuery (IDbCommand comm, params string[] parameters)
        {
            throw new NotImplementedException ();
        }
        
        private static void ExecuteNonQueryImpl (IDbCommand comm)
        {  
            commandQueue.Register (QueuedDbCommand.CreateNonQuery (comm));    
        }
        
        public static void Init ()
        {
            Init (null);
        }
        
        public static void Init (IDbConnection connection)
        {
            lock (sync) {
                commandQueue = new AsyncCommandQueue<QueuedDbCommand> (sync);
                commandQueue.QueueProcessing += OnQueueProcessing;
                commandQueue.QueueProcessed += OnQueueProcessed;
                
                disposed = false;
                conn = connection;    
            }
        }        

        public static bool Enqueue (QueuedDbCommand qdc)
        {
            // Won't check to see if it's executing.  
            // It's your own fucking fault if it is.
            if (qdc == null) {
                throw new ArgumentNullException ("qdc");
            } else if (qdc.IsCompleted) {
                throw new InvalidOperationException ("qdc already executed.");
            }
            
            lock (sync) {
                CheckDisposed ();
                return commandQueue.Register (qdc);
            }
        }

        public static bool Enqueue (IEnumerable<QueuedDbCommand> commands)
        {
            if (commands == null) {
                throw new ArgumentNullException ("commands");
            }
            
            lock (sync) {
                CheckDisposed ();
                return commandQueue.Register (commands);
            }
        }

        private static void CheckDisposed ()
        {
            if (disposed) {
                throw new ObjectDisposedException ("DatabaseManager");
            }
        }
        
        private static void CheckQueryText (string queryText)
        {
            if (String.IsNullOrEmpty (queryText)) {
            	throw new ArgumentException ("queryText:  Must not be null or empty");            	
            }            
        }  
        
        private static void OnQueueProcessing (object sender, EventArgs e)
        {
            BeginTransaction ();
        }
        
        private static void OnQueueProcessed (object sender, EventArgs e)
        {
            CommitTransaction ();
        }        
    }
}
