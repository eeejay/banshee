// 
// UserJobManager.cs
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
using System.Collections.Generic;

namespace Banshee.ServiceStack
{
    public class UserJobManager : IService, IEnumerable<IUserJob>
    {
        private List<IUserJob> user_jobs = new List<IUserJob> ();
        
        public event UserJobEventHandler JobAdded;
        public event UserJobEventHandler JobRemoved;
        
        public UserJobManager ()
        {
        }
        
        public void Register (IUserJob job)
        {
            lock (this) {
                user_jobs.Add (job);
                job.Finished += OnJobFinished;
            }
            
            OnJobAdded (job);
        }
        
        private void OnJobFinished (object o, EventArgs args)
        {
            lock (this) {
                IUserJob job = (IUserJob)o;
                
                if (user_jobs.Contains (job)) {
                    user_jobs.Remove (job);
                }
                
                job.Finished -= OnJobFinished;
                OnJobRemoved (job);
            }
        }
        
        protected virtual void OnJobAdded (IUserJob job)
        {
            UserJobEventHandler handler = JobAdded;
            if (handler != null) {
                handler (this, new UserJobEventArgs (job));
            }
        }
        
        protected virtual void OnJobRemoved (IUserJob job)
        {
            UserJobEventHandler handler = JobRemoved;
            if (handler != null) {
                handler (this, new UserJobEventArgs (job));
            }
        }
        
        public IEnumerator<IUserJob> GetEnumerator ()
        {
            return user_jobs.GetEnumerator ();
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator();
        }
    
        string IService.ServiceName {
            get { return "UserJobManager"; }
        }
    }
}
