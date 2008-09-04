// 
// UserJobTileHost.cs
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
using System.Collections.Generic;

using Gtk;

using Hyena.Widgets;
using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Gui.Widgets
{
    public class UserJobTileHost : Alignment
    {
        private AnimatedVBox box;
        private Dictionary<IUserJob, UserJobTile> job_tiles = new Dictionary<IUserJob, UserJobTile> ();
        private Dictionary<IUserJob, DateTime> job_start_times = new Dictionary<IUserJob, DateTime> ();
        
        public UserJobTileHost () : base (0.0f, 0.0f, 1.0f, 1.0f)
        {
            Banshee.Base.ThreadAssist.AssertInMainThread ();
            LeftPadding = 4;
            
            box = new AnimatedVBox ();
            box.StartPadding = 8;
            box.Spacing = 8;

            Add (box);
            ShowAll ();

            if (ServiceManager.Contains<UserJobManager> ()) {
                UserJobManager job_manager = ServiceManager.Get<UserJobManager> ();
                job_manager.JobAdded += OnJobAdded;
                job_manager.JobRemoved += OnJobRemoved;
            }

            if (ApplicationContext.CommandLine.Contains ("test-user-job")) {
                int fish;
                if (!Int32.TryParse (ApplicationContext.CommandLine["test-user-job"], out fish)) {
                    fish = 5;
                }
                TestUserJob.SpawnLikeFish (fish);
            }
        }

        private void AddJob (IUserJob job)
        {                
            lock (this) {    
                if (job == null || job.IsFinished) {
                    return;
                }
                
                if ((job.DelayShow && job.Progress < 0.33) || !job.DelayShow) {
                    Banshee.Base.ThreadAssist.AssertInMainThread ();
                    UserJobTile tile = new UserJobTile (job);
                    job_tiles.Add (job, tile);
                    job_start_times.Add (job, DateTime.Now);
                    box.PackEnd (tile, Easing.QuadraticOut);
                    tile.Show ();
                }
            }
        }
        
        private void OnJobAdded (object o, UserJobEventArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                if (args.Job.DelayShow) {
                    // Give the Job 1 second to become more than 33% complete
                    Banshee.ServiceStack.Application.RunTimeout (1000, delegate {
                        AddJob (args.Job);
                        return false;
                    });
                } else {
                    AddJob (args.Job);
                }
            });
        }
        
        private void RemoveJob (IUserJob job)
        {
            lock (this) {
                if (job_tiles.ContainsKey (job)) {
                    Banshee.Base.ThreadAssist.AssertInMainThread ();
                    UserJobTile tile = job_tiles[job];
                    box.Remove (tile);
                    job_tiles.Remove (job);
                    job_start_times.Remove (job);
                }
            }
        }
        
        private void OnJobRemoved (object o, UserJobEventArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                lock (this) {
                    if (job_start_times.ContainsKey (args.Job)) {
                        double ms_since_added = (DateTime.Now - job_start_times[args.Job]).TotalMilliseconds;
                        if (ms_since_added < 1000) {
                            // To avoid user jobs flasing up and out, don't let any job be visible for less than 1 second
                            Banshee.ServiceStack.Application.RunTimeout ((uint) (1000 - ms_since_added), delegate {
                                RemoveJob (args.Job);
                                return false;
                            });
                            return;
                        }
                    }
                    
                    RemoveJob (args.Job);
                }
            });
        }
    }
}
