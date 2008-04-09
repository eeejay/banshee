/*************************************************************************** 
 *  QueuedDbCommand.cs
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

using Migo.TaskCore;

namespace Migo.Syndication.Data
{   
    enum QueuedDbCommandType 
    {
        NonQuery,
        Scalar,
        Reader
    }

    public class QueuedDbCommand : ICommand, IDisposable
    {
        private bool completed;
        private bool executing;
        
        private object result;
        
        private ManualResetEvent execHandle;
        
        private readonly IDbCommand command;
        private readonly QueuedDbCommandType type;
        
        private readonly object sync = new object ();
        
        private ManualResetEvent ExecHandle
        {
            get {
                lock (sync) {
                    if (execHandle == null) {
                        execHandle = new ManualResetEvent (true);
                    }
                    
                    return execHandle;
                }
            }
        }
        
        public bool IsCompleted
        {
            get {
                lock (sync) {
                    return completed;
                }
            }
        }
         
        public bool IsExecuting
        {
            get {
                lock (sync) {
                    return executing;
                }
            }
        }         
         
        public IDataReader ReaderResult
        {
            get {
                WaitForResult ();
                return result as IDataReader;
            }
        }
        
        public object ScalarResult
        {
            get {
                WaitForResult ();            
                return result;
            }
        }    
        
        public object SyncRoot 
        {
            get {
                return sync;
            }
        }
        
        public static QueuedDbCommand CreateNonQuery (IDbCommand comm) 
        {
            return new QueuedDbCommand (comm, QueuedDbCommandType.NonQuery);
        }    
    
        public static QueuedDbCommand CreateScalar (IDbCommand comm) 
        {
            return new QueuedDbCommand (comm, QueuedDbCommandType.Scalar);
        }
        
        public static QueuedDbCommand CreateReader (IDbCommand comm) 
        {
            return new QueuedDbCommand (comm, QueuedDbCommandType.Reader);
        }        
        
        private QueuedDbCommand (IDbCommand comm, QueuedDbCommandType type)
        {
            if (comm == null) {
                throw new ArgumentNullException ("comm");
            }
            
            command = comm;
            this.type = type;
            
            if (type == QueuedDbCommandType.Reader ||
                type == QueuedDbCommandType.Scalar) {
                ExecHandle.Reset ();
            }
        }
        
        public void Dispose ()
        {
            lock (sync) {                    
                if (execHandle != null) {
                    execHandle.Close ();
                    execHandle = null;
                }
            }
        }
        
        public void Execute ()
        {
            if (SetExecuting ()) {
                switch (type) {
                case QueuedDbCommandType.NonQuery:
                    command.ExecuteNonQuery ();
                    break;
                case QueuedDbCommandType.Reader:
                    try {
                        result = command.ExecuteReader ();
                    } finally {
                        SetCompleted ();
                    }          
                    
                    break;
                case QueuedDbCommandType.Scalar:
                    try {
                        result = command.ExecuteScalar ();
                    } finally {
                        SetCompleted ();
                    }
                    
                    break;                
                }                
            }
        }
        
        private void SetCompleted ()
        {
            lock (sync) {
                completed = true;       
                executing = false;
                
                if (execHandle != null) {
                    execHandle.Set ();
                }
            }
        }
        
        private bool SetExecuting ()
        {
            bool ret = false;
        
            lock (sync) {
                if (!executing && !completed) {
                    ret = executing = true;
                }                  
            }
            
            return ret;
        }        
        
        private void WaitForResult ()
        {
            if (!IsCompleted) {                            
                ExecHandle.WaitOne ();
            }
        }
    }
}    