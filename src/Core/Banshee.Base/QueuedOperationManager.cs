/***************************************************************************
 *  QueuedOperationManager.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Collections;
using System.IO;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using Gtk;

using Banshee.Widgets;

namespace Banshee.Base
{
    public delegate void QueuedOperationHandler(object o, QueuedOperationArgs args);
    
    public class QueuedOperationArgs : EventArgs
    {
        public object Object;
        public string ReturnMessage;
        public bool Abort;
    }

    public class QueuedOperationManager
    {
        public class OperationCanceledException : ApplicationException
        {
        }
        
        private Queue object_queue;
        private int total_count;
        private int processed_count;
        private bool processing_queue = false;

        private ActiveUserEvent user_event;
        public ActiveUserEvent UserEvent {
            get {
                CreateUserEvent();
                return user_event;
            }
        }

        public event QueuedOperationHandler OperationRequested;
        public event EventHandler Finished;
        
        public QueuedOperationManager()
        {
            object_queue = new Queue();
        }
        
        private void CreateUserEvent()
        {
            if(user_event == null) {
                user_event = new ActiveUserEvent(ActionMessage, true);
                user_event.Icon = IconThemeUtils.LoadIcon(22, "system-search", Stock.Find);
                lock(user_event) {
                    total_count = 0;
                    processed_count = 0;
                }
            }
        }
        
        private void DestroyUserEvent()
        {
            if(user_event != null) {
                lock(user_event) {
                    user_event.Dispose();
                    user_event = null;
                    total_count = 0;
                    processed_count = 0;
                }
            }
            
            EventHandler handler = Finished;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        private void UpdateCount(string message)
        {
            CreateUserEvent();

            double new_progress = (double)processed_count / (double)total_count;
            double old_progress = user_event.Progress;
            
            if(new_progress >= 0.0 && new_progress <= 1.0 && Math.Abs(new_progress - old_progress) > 0.001) {
                string disp_progress = String.Format(ProgressMessage, processed_count, total_count);
                
                user_event.Header = disp_progress;
                user_event.Message = message;
                user_event.Progress = new_progress;
            }
        }
        
        private void CheckForCanceled()
        {
            if(user_event != null && user_event.IsCancelRequested) {
                throw new OperationCanceledException();  
            }
        }
        
        private void FinalizeOperation()
        {
            object_queue.Clear();
            processing_queue = false;
            DestroyUserEvent();
        }
        
        public void Enqueue(object obj)
        {
            CreateUserEvent();
            ThreadAssist.Spawn(delegate {
                try {
                    lock(object_queue.SyncRoot) {
                        if(object_queue.Contains(obj)) {
                            return;
                        }
                        
                        total_count++;
                        object_queue.Enqueue(obj);
                    }

                    ProcessQueue();
                } catch(OperationCanceledException) {
                    FinalizeOperation();
                }
            });
        }
        
        private void ProcessQueue()
        {
            lock(object_queue.SyncRoot) {
                if(processing_queue) {
                    return;
                } else {
                    processing_queue = true;
                }
            }

            CreateUserEvent();
            
            while(object_queue.Count > 0) {
                CheckForCanceled();
                
                object obj = object_queue.Dequeue();
                    
                QueuedOperationHandler handler = OperationRequested;
                if(handler != null && obj != null) {
                    QueuedOperationArgs args = new QueuedOperationArgs();
                    args.Object = obj;
                    handler(this, args);
                    processed_count++;
                    
                    if(handle_user_event) {
                        UpdateCount(args.ReturnMessage);
                    }
                    
                    if(args.Abort) {
                        break;
                    }
                } else if(handle_user_event) {
                    processed_count++;
                    UpdateCount(null);
                } else {
                    processed_count++;
                }
            }

            object_queue.Clear();
            processing_queue = false;
            
            DestroyUserEvent();
        }

        private string action_message;
        public string ActionMessage {
            get { return action_message; }
            set { action_message = value; }
        }
        
        private string progress_message;
        public string ProgressMessage {
            get { return progress_message; }
            set { progress_message = value; }
        }
        
        private bool handle_user_event = true;
        public bool HandleActveUserEvent {
            get { return handle_user_event; }
            set { handle_user_event = value; }
        }
        
        public int TotalCount {
            get { return total_count; }
        }
        
        public int ProcessedCount { 
            get { return processed_count; }
        }
    }
}

