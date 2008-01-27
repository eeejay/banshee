/*
 *  Copyright (c) 2006 Sebastian Dröge <slomo@circular-chaos.org> 
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
using Banshee.Base;
using Banshee.Widgets;
using Mono.Unix;
using Gtk;

namespace Banshee.PlayerMigration
{
    public abstract class PlayerImport
    {
        protected ActiveUserEvent user_event;

        public PlayerImport ()
        {
        }

        protected void CreateUserEvent ()
        {
            user_event = new ActiveUserEvent (Catalog.GetString("Importing Songs"));
            user_event.CancelMessage = Catalog.GetString("The import process is currently running. Would you like to stop it?");
            user_event.Message = Catalog.GetString("Scanning for songs");
            user_event.Icon = IconThemeUtils.LoadIcon(22, "system-search", Stock.Find);
            user_event.Progress = 0.0;
            user_event.CanCancel = true;
        }

        protected void UpdateUserEvent (int processed, int count, string artist, string title)
        {
            double old_progress = user_event.Progress, new_progress = ((double) processed) / ((double) count);
            user_event.Message = String.Format("{0} - {1}", artist, title);
            
            if (new_progress >= 0.0 && new_progress <= 1.0 && Math.Abs(new_progress - old_progress) > 0.001) {
                string disp_progress = String.Format(Catalog.GetString("Importing {0} of {1}"),
                    processed, count);
                    
                user_event.Header = disp_progress;
                user_event.Progress = new_progress;
            }
        }

        public void Import ()
        {
            ThreadAssist.Spawn (delegate {
                CreateUserEvent ();
                using (user_event) {
                    OnImport ();
                }
            });
        }

        protected abstract void OnImport ();

        public abstract bool CanImport { get; }
        public abstract string Name { get; }
    }
}
