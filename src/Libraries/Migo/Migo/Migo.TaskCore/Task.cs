/***************************************************************************
 *  Task.cs
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
using System.Threading;
using System.ComponentModel;

namespace Migo.TaskCore
{
    public abstract class Task
    {    
        private string name;
        private int progress;        
        
        private Guid groupID;
        private TaskEventPipeline pipeline;
        
        private TaskStatus status;
        
        private readonly object userState;        
        private readonly object syncRoot = new object ();            
        
        public EventHandler<TaskCompletedEventArgs> Completed;                        
        public EventHandler<ProgressChangedEventArgs> ProgressChanged;            
        public EventHandler<TaskStatusChangedEventArgs> StatusChanged;            
        
        public bool IsCompleted 
        {
            get {
                bool ret = false;
                TaskStatus status = Status;
                
                if (status == TaskStatus.Cancelled ||
                    status == TaskStatus.Failed    ||
                    status == TaskStatus.Succeeded ||
                    status == TaskStatus.Stopped
                ) {
                    ret = true;
                }   
                
                return ret;
            }
        }
        
        public string Name 
        {
            get { return name; }
            set { name = value; }
        }
        
        public TaskEventPipeline EventPipeline
        {
            get { return pipeline; }
            set { pipeline = value; }
        }
        
        public int Progress 
        {
            get { return progress; }
            protected set {
                SetProgress (value);
            }
        }   

        public TaskStatus Status 
        {
            get { return status; }
            protected set { SetStatus (value); } 
        }

        public object SyncRoot 
        {
            get { return syncRoot; }
        }        
        
        public object UserState {
            get { return userState; }
        }
        
        public abstract WaitHandle WaitHandle {
            get;
        }        
     
        internal Guid GroupID {
            get { return groupID; }
            set { groupID = value; }
        }

        protected Task () : this (String.Empty, null, null) {}
        protected Task (string name, TaskEventPipeline pipeline, object userState)
        {
            this.pipeline = pipeline; 
        
            groupID = Guid.Empty;                                    
            this.name = name;
            progress = 0;
            status = TaskStatus.Ready;
            this.userState = userState;
        }     
        
        public abstract void CancelAsync ();
        
        public virtual void Pause ()
        {
            throw new NotImplementedException ("Pause");
        }

        public virtual void Resume ()
        {
            throw new NotImplementedException ("Resume");
        }

        public virtual void Stop ()
        {
            throw new NotImplementedException ("Stop");            
        }
        
        public abstract void ExecuteAsync ();


        public override string ToString ()
        {
            return Name;  
        }

        protected virtual void SetProgress (int progress)
        {
            if (progress < 0 || progress > 100) {
                throw new ArgumentOutOfRangeException ("progress");
            } else if (this.progress != progress) {
                this.progress = progress;
                OnProgressChanged (progress);
            }
        }

        protected internal virtual void SetStatus (TaskStatus status)
        {
            SetStatus (status, true);              
        }                
                
        protected internal virtual void SetStatus (TaskStatus status, 
                                                   bool emitStatusChangedEvent)
        {
            if (this.status != status) {
                TaskStatus oldStatus = this.status;
                this.status = status;
                
                if (emitStatusChangedEvent) {
                    OnStatusChanged (oldStatus, status);
                }
            }                
        }

        protected virtual void OnProgressChanged (int progress)
        {
            OnProgressChanged (
                new ProgressChangedEventArgs (progress, userState)
            );
        }

        protected virtual void OnProgressChanged (ProgressChangedEventArgs e)
        {
            TaskEventPipeline pipelineCpy = pipeline;
            EventHandler<ProgressChangedEventArgs> handler = ProgressChanged;
            
            if (pipelineCpy != null) {
                pipelineCpy.RegisterCommand (new CommandWrapper (delegate {     
                    pipelineCpy.OnTaskProgressChanged (this, e);
                    
                    if (handler != null) {
                        handler (this, e);                
                    }
                }));                    
            } else if (handler != null) {
                handler (this, e);
            }
        }

        protected virtual void OnStatusChanged (TaskStatus oldStatus, 
                                                TaskStatus newStatus)
        {
            OnStatusChanged (
                new TaskStatusChangedInfo (this, oldStatus, newStatus)
            );
        }

        protected virtual void OnStatusChanged (TaskStatusChangedInfo tsci)
        {
            TaskEventPipeline pipelineCpy = pipeline;
            EventHandler<TaskStatusChangedEventArgs> handler = StatusChanged;

            if (pipelineCpy != null) {
                pipelineCpy.RegisterCommand (new CommandWrapper (delegate {            
                    TaskStatusChangedEventArgs e = 
                        new TaskStatusChangedEventArgs (tsci);
                        
                    pipelineCpy.OnTaskStatusChanged (e);
                    
                    if (handler != null) {
                        handler (this, e);               
                    }
                }));
            } else if (handler != null) {
                handler (this, new TaskStatusChangedEventArgs (tsci));
            }
        }
  
        protected virtual void OnTaskCompleted (Exception error, bool cancelled)
        {
            OnTaskCompleted (new TaskCompletedEventArgs (
                error, cancelled, userState
            ));               
        }
        
        protected virtual void OnTaskCompleted (TaskCompletedEventArgs e) 
        {
            TaskEventPipeline pipelineCpy = pipeline;
            EventHandler<TaskCompletedEventArgs> handler = Completed;

            if (pipelineCpy != null) {
                pipelineCpy.RegisterCommand (new CommandWrapper (delegate {
                    pipelineCpy.OnTaskCompleted (this, e);
                    
                    if (handler != null) {
                        handler (this, e);
                    }
                }));
            } else if (handler != null) {
                handler (this, e);
            }
        }  
    }
}
