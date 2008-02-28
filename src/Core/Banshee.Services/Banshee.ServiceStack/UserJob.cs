// 
// UserJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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

using Hyena.Data;

namespace Banshee.ServiceStack
{
    public class UserJob : IUserJob
    {
        private string name;
        private string title;
        private string status;
        private double progress;
        private string [] icon_names;
        private string cancel_message;
        private bool can_cancel;
        private bool is_cancel_requested;
        private bool is_finished;
        private bool delay_show;
        
        private int update_freeze_ref;
        
        public event EventHandler Finished;
        public event EventHandler Updated;
        public event EventHandler CancelRequested;
        
        public UserJob (string name, string title, string status) : this (name, title, status, null)
        {
        }
        
        /*public UserJob (string name, string title, string status, string iconName)
            : this (name, title, status, new string [] { iconName })
        {
        }*/
        
        public UserJob (string name, string title, string status, params string [] iconNames)
        {
            FreezeUpdate ();
            Name = name;
            Title = title;
            Status = status;
            IconNames = iconNames;
            ThawUpdate (true);
        }
        
        public void Register ()
        {
            if (ServiceManager.Contains ("UserJobManager")) {
                ServiceManager.Get<UserJobManager> ("UserJobManager").Register (this);
            }
        }
        
        public void Cancel ()
        {
            OnCancelRequested ();
        }
        
        public void Finish ()
        {
            is_finished = true;
            OnFinished ();
        }
        
        protected void FreezeUpdate ()
        {
            Interlocked.Increment (ref update_freeze_ref);
        }
        
        protected void ThawUpdate (bool raiseUpdate)
        {
            Interlocked.Decrement (ref update_freeze_ref);
            if (raiseUpdate) {
                OnUpdated ();
            }
        }
        
        protected virtual void OnFinished ()
        {
            EventHandler handler = Finished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnUpdated ()
        {
            if (update_freeze_ref != 0) {
                return;
            }
            
            EventHandler handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnCancelRequested ()
        {
            IsCancelRequested = true;
            
            EventHandler handler = CancelRequested;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public virtual string Name {
            get { return name; }
            set { name = value; }
        }
        
        public virtual string Title {
            get { return title; }
            set { 
                title = value; 
                OnUpdated (); 
            }
        }
        
        public virtual string Status {
            get { return status; }
            set { 
                status = value; 
                OnUpdated (); 
            }
        }
        
        public virtual double Progress {
            get { return progress; }
            set { 
                progress = Math.Max (0.0, Math.Min (1.0, value)); 
                OnUpdated (); 
            }
        }
        
        public virtual string [] IconNames {
            get { return icon_names; }
            set {
                if (value != null) {
                    icon_names = value; 
                    OnUpdated (); 
                }
            }
        }
        
        public virtual string CancelMessage {
            get { return cancel_message; }
            set { cancel_message = value; }
        }
        
        public virtual bool CanCancel {
            get { return can_cancel; }
            set {
                can_cancel = value;
                OnUpdated ();
            }
        }
        
        public virtual bool IsCancelRequested {
            get { return is_cancel_requested; }
            set { is_cancel_requested = value; }
        }
        
        public virtual bool IsFinished {
            get { return is_finished; }
        }
        
        public virtual bool DelayShow {
            get { return delay_show; }
            set { delay_show = value; }
        }
    }
}
