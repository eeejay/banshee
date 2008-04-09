/***************************************************************************
 *  TaskGroup.cs
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
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLgsm, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;

using Migo.TaskCore.Collections;

namespace Migo.TaskCore
{        
    public class TaskGroup<T> : TaskEventPipeline where T : Task
    {         
        private bool disposed;
        private bool executing;
        private bool cancelRequested;
                
        private readonly Guid id;                      
        private readonly object sync;        
        
        private AsyncCommandQueue<ICommand> commandQueue;        
        
        private List<T> currentTasks;        
        private TaskCollection<T> tasks;            
           
        private GroupStatusManager<T> gsm;
        private GroupProgressManager<T> gpm;
        
        // Used to notify user after stopped event has fired
        private ManualResetEvent execHandle = new ManualResetEvent (true);
        
        // mildly redundant but necessary
        // Used to notify system when all tasks are completed       
        private AutoResetEvent executingHandle = new AutoResetEvent (false);
        
        public event EventHandler<EventArgs> Started;
        public event EventHandler<EventArgs> Stopped;
        
        public event EventHandler<TaskEventArgs<T>> TaskStarted;
        public event EventHandler<TaskEventArgs<T>> TaskStopped;        
        public event EventHandler<TaskEventArgs<T>> TaskAssociated;
        
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler<GroupStatusChangedEventArgs> StatusChanged;        

        public event EventHandler<ProgressChangedEventArgs> TaskProgressChanged;
        public event EventHandler<TaskStatusChangedEventArgs> TaskStatusChanged;        

        public virtual int CompletedTasks
        {
            get {
                lock (sync) {
                    CheckDisposed ();
                    return gsm.CompletedTasks;
                }
            }
        }
        
        protected virtual bool Disposed 
        {
            get { 
                lock (sync) {
                    return disposed;
                }
            }
        }
        
        public virtual bool IsExecuting
        {
            get {
                lock (sync) {
                    CheckDisposed ();
                    return executing;
                }
            }
        }
        
        public Guid ID {
            get { 
                lock (sync) {            
                    CheckDisposed ();                
                    return id;
                }
            }
        }
        
        public virtual int RunningTasks 
        {
            get {
                lock (sync) {            
                    CheckDisposed ();
                    return gsm.RunningTasks;
                }
            }
        }                
        
        public virtual TaskCollection<T> Tasks
        {
            get {
                lock (sync) {
                    CheckDisposed ();
                    return tasks;
                }
            }
        }

        public virtual int RemainingTasks 
        {
            get {
                lock (sync) {                
                    CheckDisposed ();
                    return gsm.RemainingTasks;
                }
            }
        }     

        protected GroupProgressManager<T> ProgressManager
        {
            get {
                return gpm;
            }
            
            set {
                SetProgressManager (value); 
            }
        }

        protected GroupStatusManager<T> StatusManager 
        {
            get { 
                if (gsm == null) {
                    SetStatusManager (new GroupStatusManager<T> ());
                }
                
                return gsm;
            }
            
            set { 
                SetStatusManager (value); 
            }
        }

        public IEnumerable<T> CurrentTasks 
        {
            get { 
                lock (sync) {
					CheckDisposed ();					
                    return currentTasks as IEnumerable<T>;
                }
            }
        }

        public int MaxRunningTasks 
        {
            get {
                lock (sync) {
                    CheckDisposed ();                
                    return gsm.MaxRunningTasks; 
                }
            }
            
            set { 
                lock (sync) {
                    CheckDisposed ();                
                    gsm.MaxRunningTasks = value;
                }
            }
        }      

        public WaitHandle Handle 
        {
            get { 
                lock (sync) {
                    CheckDisposed ();            
                    return execHandle;
                }
            }
        }

        public object SyncRoot
        {
            get {             
                return sync;
            }
        }
        
        public TaskGroup (int maxRunningTasks, 
                          TaskCollection<T> tasks)
            : this (maxRunningTasks, tasks, null, null) {}

        public TaskGroup (int maxRunningTasks, 
                          TaskCollection<T> tasks, 
                          GroupStatusManager<T> statusManager) 
            : this (maxRunningTasks, tasks, statusManager, null) {}
        
        protected TaskGroup (int maxRunningTasks, 
                             TaskCollection<T> tasks, 
                             GroupStatusManager<T> statusManager,
                             GroupProgressManager<T> progressManager) 
        {
            if (maxRunningTasks < 0) {
                throw new ArgumentException ("maxRunningTasks must be >= 0");
            } else if (tasks == null) {
                throw new ArgumentNullException ("tasks");
            }

            id = Guid.NewGuid ();
            sync = tasks.SyncRoot;
            commandQueue = new AsyncCommandQueue<ICommand> ();
            currentTasks = new List<T> (maxRunningTasks);
                     
            SetProgressManager (
                progressManager ?? new GroupProgressManager<T> ()
            );            
                        
            SetStatusManager (
                statusManager ?? new GroupStatusManager<T> ()
            );  
                        
            try {
                gsm.SuspendUpdate = true;
                
                gsm.RemainingTasks = tasks.Count;
                gsm.MaxRunningTasks = maxRunningTasks;               
                                
                SetTaskCollection (tasks);
            } finally {
                gsm.SuspendUpdate = false;
                gsm.Update ();
            }         
        }
   
        public virtual void CancelAsync ()
        {
            if (SetCancelled ()) {
                List<T> tasksCpy = null;

                lock (sync) {                    
                    tasksCpy = new List<T> (tasks);

                    foreach (T task in tasksCpy) {
                        task.CancelAsync ();
                    }
                }                      
            }
        }
        
        public virtual void StopAsync ()
        {
            if (SetCancelled ()) {
                List<T> tasksCpy = null;

                lock (sync) {                    
                    tasksCpy = new List<T> (tasks);
                    
                    foreach (T task in tasksCpy) {
                        task.Stop ();
                    }
                }                      
            } 
        }        
        
		public virtual void Dispose ()
		{
		    Dispose (null);
		}
		
        public virtual void Dispose (AutoResetEvent handle)
        {
            if (SetDisposed ()) { 
                commandQueue.Dispose ();                                
                
                try {
                    lock (sync) {
                        tasks.TaskAdded -= OnTaskAddedHandler;            
                        tasks.TaskRemoved -= OnTaskRemovedHandler; 
                        tasks.CommandQueue = null;

                        if (tasks.Count > 0) {
                            Disassociate (tasks);
                        }                          
                    }    
                    
                    if (gpm != null) {
                        gpm.Group = null;
                    }
                    
                    if (gsm != null) {                    
                        gsm.Group = null;
                    }                    
                    
                    gsm.Dispose ();
                    gpm.Reset ();
                } finally {
                    gpm = null;
                    gsm = null;
                    tasks = null;
                }            
            
                if (executingHandle != null) {
                    executingHandle.Close ();
                    executingHandle = null;                    
                }
                
                if (execHandle != null) {
                    execHandle.Close ();
                    execHandle = null;
                }
            }
            
            if (handle != null) {
                handle.Set ();
            }
        }

        public void Execute ()
        {
            if (SetExecuting (true)) {                
                try {
                    OnStarted ();
                    SpawnExecutionThread ();
                } catch {
                    OnStopped ();
                    Reset ();                                                            
                    SetExecuting (false);
                }
            }
        }

        protected virtual bool SetCancelled ()
        {
            bool ret = false;
            
            lock (sync) {
                CheckDisposed ();
                
                if (executing && !cancelRequested) {
                    ret = cancelRequested = true;
                }
            }
            
            return ret;
        }
        
        private bool SetDisposed ()
        {
            bool ret = false;
                
            lock (sync) {
                if (!disposed) {
                    ret = disposed = true;   
                }
            }
                
            return ret;
        }
        
        protected virtual bool SetExecuting (bool exec)
        {
            bool ret = false;
            
            lock (sync) {
                CheckDisposed ();
                
                if (exec) {
                    if (!executing && !cancelRequested) {
                        ret = executing = true;
                    }
                } else {
                    executing = false;
                    cancelRequested = false;
                }
            }
            
            return ret;
        }

/*  May implement at some point        
        protected virtual void HoldStatusUpdates ()
        {
            lock (sync) {
                queueStatusUpdates = true;
            }
        }
        
        protected virtual void ProcessStatusUpdates ()
        {
            lock (sync) {
                queueStatusUpdates = false;
                
                TaskStatusChangedInfo[] changeInfo = queuedStatusUpdates.ToArray ();
                queuedStatusUpdates.Clear ();
                
                OnTaskStatusChanged (
                    new TaskStatusChangedEventArgs (changeInfo)
                );
            }
        }        
*/        

        protected virtual void SetProgressManager (GroupProgressManager<T> progressManager) 
        {
            CheckDisposed ();        
            
            if (progressManager == null) {
                throw new ArgumentNullException ("progressManager");
            } else if (gpm != null) {
                throw new InvalidOperationException ("ProgressManager already set");
            }
                                
            gpm = progressManager;
            gpm.Group = this;
        }
        
        protected virtual void SetStatusManager (GroupStatusManager<T> statusManager) 
        {
            CheckDisposed ();
            
            if (statusManager == null) {
                throw new ArgumentNullException ("statusManager");
            } else if (gsm != null) {
                throw new InvalidOperationException ("StatusManager already set");
            }
            
            gsm = statusManager;
            gsm.Group = this;
        }
        
        protected virtual void SetTaskCollection (TaskCollection<T> collection)
        {
            CheckDisposed ();                
    
            if (tasks != null) {
                throw new InvalidOperationException (
                    "Already associated with a task collection"
                );
            }
    
            tasks = collection;
            tasks.CommandQueue = commandQueue;
            
            tasks.TaskAdded += OnTaskAddedHandler;            
            tasks.TaskRemoved += OnTaskRemovedHandler; 
            
            Associate (tasks);                   
        }        

        protected virtual void CheckDisposed ()
        {
            if (disposed) {
                throw new ObjectDisposedException (GetType ().FullName);
            }
        }

        protected virtual void Associate (T task)
        {
            Associate (task, true);
        }
                
        protected virtual void Associate (IEnumerable<T> tasks)
        {          
            foreach (T task in tasks) {
                if (task != null) {
                    Associate (task, false);
                }
            }
            
            gpm.Add (tasks);
        }

        protected virtual void Associate (T task, bool addToProgressGroup)
        {            
            CheckDisposed ();            

            if (task.GroupID.CompareTo (Guid.Empty) != 0) {
                throw new ApplicationException (
                    "Task already associated with a group"
                );
            }             
            
            task.GroupID = id;
            task.EventPipeline = this;

            if (addToProgressGroup) {
                gpm.Add (task);
            }      
        }

        protected virtual bool CheckID (T task)
        {
            return (task.GroupID.CompareTo (id) == 0); 
        }

        protected virtual void Disassociate (IEnumerable<T> tasks)
        {
            foreach (T task in tasks) {
                Disassociate (task, false);
            }

            if (gpm != null) {
                gpm.Remove (tasks);
            }
        }

        protected virtual void Disassociate (T task)
        {
            Disassociate (task, true);
        }
        
        protected virtual void Disassociate (T task, bool removeFromProgressGroup)
        {
            if (CheckID (task)) {
                if (removeFromProgressGroup && gpm != null) {       
                    gpm.Remove (task);
                }
                
                task.GroupID = Guid.Empty;
                task.EventPipeline = null;
            }
        }    

        private bool Done ()
        {
            return (Disposed || gsm.RemainingTasks == 0) ? true : false;
        }

        protected virtual void Reset ()
        {
            lock (sync) {
                gsm.Reset ();
                gpm.Reset ();
            }
        }

        protected virtual void OnStarted ()
        {
            lock (sync) {
                executingHandle.Reset ();
                execHandle.Reset ();
                OnEvent (Started);
            }
        }
        
        protected virtual void OnStopped ()
        {
            lock (sync) {
                OnEvent (Stopped);
                execHandle.Set ();                
            }            
        }

        protected virtual void OnTaskStarted (T task)
        {
            lock (sync) {
                OnTaskEvent (task, TaskStarted);
            }
        }

        protected virtual void OnTaskStopped (T task)
        {
            lock (sync) {
                if (currentTasks.Contains (task)) {
                    currentTasks.Remove (task);
                }

                OnTaskEvent (task, TaskStopped);
            }
        }
        
        protected internal virtual void OnStatusChanged (GroupStatusChangedEventArgs e)        
        {
            lock (sync) {
                if (!cancelRequested) {
                    EventHandler<GroupStatusChangedEventArgs> handler = StatusChanged;

                    if (handler != null) {
                    	commandQueue.Register (
                    	    new EventWrapper<GroupStatusChangedEventArgs> (
                    	        handler, this, e
                    	    )
                    	);
                	}
                }
            }
        }

        protected internal virtual void OnStatusChanged (object sender, 
                                                         GroupStatusChangedEventArgs e)
        {
            OnStatusChanged (e);
        }

        protected internal override void RegisterCommand (ICommand command)
        {
            AsyncCommandQueue<ICommand> cmdQCpy = commandQueue;
            
            if (cmdQCpy != null && command != null) {
            	cmdQCpy.Register (command);
            }
        }

        protected internal override void OnTaskCompleted (object sender, 
                                                          TaskCompletedEventArgs e)
        {   
            lock (sync) {
                T t = sender as T;
                
                try {
                    gsm.SuspendUpdate = true;
                    
                    if (currentTasks.Contains (t)) {
                        gsm.DecrementRunningTaskCount ();
                        
                        if (t.IsCompleted) {
                            if (!e.Cancelled) {
                                gsm.IncrementCompletedTaskCount ();
                            }           
                        }
                    }
                    
                    if (t.IsCompleted) {
                        gsm.DecrementRemainingTaskCount ();
                    }
                } finally {
                    gsm.SuspendUpdate = false;
                    gsm.Update ();
                    OnTaskStopped (t);                                
                }
            }
        }       
        
        protected internal override void OnTaskProgressChanged (object sender, 
                                                                ProgressChangedEventArgs e)
        {     
            EventHandler<ProgressChangedEventArgs> handler = TaskProgressChanged;                
                
            lock (sync) {
                if (!cancelRequested) {
                    gpm.Update (sender as T, e.ProgressPercentage);
                }
            }   
            
            if (handler != null) {
                handler (this, e);          
            }                           
        }        
        
        protected internal override void OnTaskStatusChanged (TaskStatusChangedEventArgs e)
        {         
            EventHandler<TaskStatusChangedEventArgs> handler = TaskStatusChanged;                
                
            if (handler != null) {
                handler (this, e);        
            }            
            
            gsm.Evaluate ();       
        } 
        
        protected virtual void OnTaskAddedHandler (object sender, 
                                                   TaskAddedEventArgs<T> e)
        {        
            lock (sync) {
                if (e.Task != null) {
                    Associate (e.Task);
                    gsm.IncrementRemainingTaskCount ();
                    OnTaskAssociated (e.Task);    
                } else if (e.Tasks != null) {
                    Associate (e.Tasks);
                    gsm.RemainingTasks += e.Tasks.Count;
                    OnTaskAssociated (e.Tasks);
                }  
            }
        }        
        
        protected virtual void OnTaskRemovedHandler (object sender, 
                                                     TaskRemovedEventArgs<T> e)
        {
            lock (sync) {
                if (e.Index != -1 && e.Task != null) {
                    T tsk = e.Task as T;
                    
                    if (currentTasks.Contains (tsk)) {
                        tsk.CancelAsync ();
                        return;
                    } else if (tsk.Status == TaskStatus.Ready || 
                               tsk.Status == TaskStatus.Paused) {
                        gsm.DecrementRemainingTaskCount ();
                    } 
                    
                    Disassociate (tsk);                    
                } else if (e.Indices != null && e.Tasks != null) {           
                    int sub = e.Tasks.Count;
                    List<T> removeList = new List<T> (sub);
                    
                    foreach (T tsk in e.Tasks) {
                        if (currentTasks.Contains (tsk)) {
                            tsk.CancelAsync ();
                            --sub;
                        } else {
                            removeList.Add (tsk);
                        }
                    }
                    
                    if (sub > 0) {
                        Disassociate (removeList);                        
                        gsm.RemainingTasks -= sub;
                    }
                }
            }
        }

        protected internal virtual void OnProgressChanged (ProgressChangedEventArgs e)
        {
            EventHandler<ProgressChangedEventArgs> handler = ProgressChanged;
            
            if (handler != null) {
                handler (this, e);   
            }
        }

        protected virtual void OnTaskAssociated (T task)
        {
            OnTaskEvent (task, TaskAssociated);
        }
        
        protected virtual void OnTaskAssociated (IEnumerable<T> tasks) 
        {
            OnTaskEvent (tasks, TaskAssociated);            
        }
        
        private void OnEvent (EventHandler<EventArgs> eventHandler)
        {
            EventHandler<EventArgs> handler = eventHandler;
        
            if (handler != null) {
            	commandQueue.Register (new EventWrapper<EventArgs> (
            	    handler, this, new EventArgs ())
            	);
            }                
        }

        private void OnTaskEvent (T task, EventHandler<TaskEventArgs<T>> eventHandler)
        {
            EventHandler<TaskEventArgs<T>> handler = eventHandler;
        
            if (handler != null) {
            	commandQueue.Register (new EventWrapper<TaskEventArgs<T>> (
            	    handler, this, new TaskEventArgs<T> (task))
            	);
            }                         
        }

        private void OnTaskEvent (IEnumerable<T> tasks, 
                                  EventHandler<TaskEventArgs<T>> eventHandler)
        {
            EventHandler<TaskEventArgs<T>> handler = eventHandler;
        
            if (handler != null) {
            	commandQueue.Register (new EventWrapper<TaskEventArgs<T>> (
            	    handler, this, new TaskEventArgs<T> (tasks))
            	);
            }                         
        }        
        
        private void PumpQueue ()
        {
            try {
                PumpQueueImpl (); 
            } finally {
                executingHandle.WaitOne ();
                
                lock (sync) {
                    SetExecuting (false); 
                    OnStopped ();
                    Reset ();
                }
            }
        }
                
        private void PumpQueueImpl ()
        {
            T task;
            
            while (true) {
                gsm.Wait ();
           
                lock (sync) {
                    gsm.ResetWait ();     
                    
                    if (Done ()) {
                        executingHandle.Set ();                                                
                        return;
                    } else if (cancelRequested) {
                        continue;
                    }
                    
                    task = null;
                    ITaskCollectionEnumerator<T> tce = Tasks.GetEnumerator ()
                        as ITaskCollectionEnumerator<T>;   
                        
                    if (tce.MoveFirst (TaskStatus.Ready)) {
                        task = tce.Current;
  
                        try {                            
                            lock (task.SyncRoot) { 
                                if (task.Status != TaskStatus.Ready) {
                                	continue;
                                }
                                
                                currentTasks.Add (task);
                                gsm.IncrementRunningTaskCount ();
                            
                                OnTaskStarted (task);
                                task.ExecuteAsync ();
                            }
                        } catch (Exception e) {        
                            Console.WriteLine ("PumpQueue Exception:  {0}", e.Message);
                            Console.WriteLine (e.StackTrace);
                            
                            try {
                                gsm.SuspendUpdate = true;
                                gsm.DecrementRunningTaskCount ();
                                gsm.IncrementCompletedTaskCount ();
                            } finally {
                                OnTaskStopped (task);                             
                                gsm.SuspendUpdate = false;
                                gsm.Update ();
                            } 
                        }
                    }
                }
            }
        } 
        
        private void SpawnExecutionThread ()
        {                     
            Thread t = new Thread (new ThreadStart (PumpQueue));
            t.Priority = ThreadPriority.Normal;
            t.IsBackground = true;
            t.Start ();       
        }        
    }
    
    // This is retarded, I know.  But there is no fucking way to do 
    // a clean implementation because of generic inheritance.  
    // I.E.  There is no way for a task to hold a reference to its group
    // w\o this.  If you know of a way, please tell me.
    public abstract class TaskEventPipeline
    {
        protected internal abstract void RegisterCommand (ICommand command);
    
        protected internal abstract void OnTaskCompleted (
            object sender, TaskCompletedEventArgs e
        );    
        
        protected internal abstract void OnTaskProgressChanged (
            object sender, ProgressChangedEventArgs e
        );
        
        protected internal abstract void OnTaskStatusChanged (TaskStatusChangedEventArgs e);        
    }
    
}
