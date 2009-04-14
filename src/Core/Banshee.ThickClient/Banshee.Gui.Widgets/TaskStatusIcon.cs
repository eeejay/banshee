// 
// TaskStatusIcon.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;

using Mono.Unix;

using Gtk;

using Hyena;
using Hyena.Widgets;

using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Gui.Widgets
{
    public class TaskStatusIcon : AnimatedImage
    {
        //private Dictionary<IUserJob, UserJobTile> job_tiles = new Dictionary<IUserJob, UserJobTile> ();
        private List<IUserJob> jobs = new List<IUserJob> ();

        public TaskStatusIcon ()
        {
            // Setup widgetry
            try {
                Pixbuf = Gtk.IconTheme.Default.LoadIcon ("process-working", 22, IconLookupFlags.NoSvg);
                FrameHeight = 22;
                FrameWidth = 22;
                Load ();
                TaskActive = false;
            } catch (Exception e) {
                Hyena.Log.Exception (e);
            }

            // Listen for jobs
            UserJobManager job_manager = ServiceManager.Get<UserJobManager> ();
            job_manager.JobAdded += OnJobAdded;
            job_manager.JobRemoved += OnJobRemoved;

            Update ();
        }

        private void Update ()
        {
            lock (jobs) {
                if (jobs.Count > 0) {
                    StringBuilder sb = new StringBuilder ();
                    foreach (IUserJob job in jobs) {
                        sb.AppendFormat ("\n<i>{0}</i>", job.Title);
                    }

                    TooltipMarkup = String.Format ("<b>{0}</b>{1}",
                        String.Format (
                            // Translators: the number of jobs running is available for your use via {0}
                            Catalog.GetPluralString ("Background Task Running:", "Background Tasks Running:", jobs.Count),
                            jobs.Count
                        ), sb.ToString ()
                    );
                    TaskActive = true;
                } else {
                    TooltipText = Catalog.GetString ("No background tasks running");
                    TaskActive = false;
                }
            }
        }

        private bool first = true;
        private bool task_active = false;
        private bool TaskActive {
            set {
                if (!first && task_active == value)
                    return;

                first = false;
                task_active = value;

                if (task_active) {
                    TurnOn ();
                } else {
                    TurnOff ();
                }
            }
        }

        private bool TurnOn ()
        {
            if (task_active) {
                Active = true;
                Sensitive = true;
                Banshee.ServiceStack.Application.RunTimeout (1000, TurnOff);
            }
            return false;
        }

        private bool TurnOff ()
        {
            Active = false;
            Sensitive = task_active;

            if (task_active) {
                Banshee.ServiceStack.Application.RunTimeout (5000, TurnOn);
            }
            return false;
        }

        private void AddJob (IUserJob job)
        {                
            lock (jobs) {    
                if (job == null || !job.IsBackground || job.IsFinished) {
                    return;
                }
                
                jobs.Add (job);
            }

            ThreadAssist.ProxyToMain (Update);
        }
        
        private void OnJobAdded (object o, UserJobEventArgs args)
        {
            AddJob (args.Job);
        }
        
        private void RemoveJob (IUserJob job)
        {
            lock (jobs) {
                if (jobs.Contains (job)) {
                    jobs.Remove (job);
                }
            }

            ThreadAssist.ProxyToMain (Update);
        }
        
        private void OnJobRemoved (object o, UserJobEventArgs args)
        {
            RemoveJob (args.Job);
        }
    }
}
