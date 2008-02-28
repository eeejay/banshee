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

using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Gui.Widgets
{
    public class UserJobTileHost : Alignment
    {
        private VBox box;
        private Dictionary<IUserJob, UserJobTile> job_tiles = new Dictionary<IUserJob, UserJobTile> ();
        
        public UserJobTileHost () : base (0.0f, 0.0f, 1.0f, 1.0f)
        {
            box = new VBox ();
            box.Spacing = 8;
            box.Show ();

            Add (box);

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

        public new void Show ()
        {
            TopPadding = 8;
            base.Show ();
        }

        public new void Hide ()
        {
            TopPadding = 0;
            base.Hide ();
        }

        private void AddJob (IUserJob job)
        {                    
            if (job == null || job.IsFinished) {
                return;
            }
            
            if ((job.DelayShow && job.Progress < 0.33) || !job.DelayShow) {
                UserJobTile tile = new UserJobTile (job);
                job_tiles.Add (job, tile);
                box.PackStart (tile, false, false, 0);
                tile.Show ();
                Show ();
            }
        }
        
        private void OnJobAdded (object o, UserJobEventArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                lock (this) {
                    if (args.Job.DelayShow) {
                        // Give the Job 1 second to become more than 33% complete
                        Banshee.ServiceStack.Application.RunTimeout (1000, delegate {
                            AddJob (args.Job);
                            return false;
                        });
                    } else {
                        AddJob (args.Job);
                    }
                }
            });
        }
        
        private void OnJobRemoved (object o, UserJobEventArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                lock (this) {
                    if (job_tiles.ContainsKey (args.Job)) {
                        UserJobTile tile = job_tiles[args.Job];
                        box.Remove (tile);
                        job_tiles.Remove (args.Job);
                    }
    
                    if (job_tiles.Count <= 0) {
                        Hide ();
                    }
                }
            });
        }
    }
}
