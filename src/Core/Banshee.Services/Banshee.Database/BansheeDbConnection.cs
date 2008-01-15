//
// BansheeDbConnection.cs
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
using System.IO;
using System.Data;
using Mono.Data.Sqlite;

using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Database
{
    public sealed class BansheeDbConnection : HyenaSqliteConnection, IService
    {
        public BansheeDbConnection () : this (true)
        {
        }

        public BansheeDbConnection (bool connect)
            : base(connect)
        {
            if (connect) {
                BansheeDbFormatMigrator migrator = new BansheeDbFormatMigrator (Connection);
                migrator.SlowStarted += OnMigrationSlowStarted;
                migrator.SlowPulse += OnMigrationSlowPulse;
                migrator.SlowFinished += OnMigrationSlowFinished;
                migrator.Migrate ();
            }
        }
        
        //private Gtk.Window slow_window;
        //private Gtk.ProgressBar slow_progress;
        
        private void IterateSlow ()
        {
            /*while (Gtk.Application.EventsPending ()) {
                Gtk.Application.RunIteration ();
            }*/
        }
        
        private void OnMigrationSlowStarted (string title, string message)
        {
           /* lock (this) {
                if (slow_window != null) {
                    slow_window.Destroy ();
                }
                
                Gtk.Application.Init ();
                
                slow_window = new Gtk.Window (String.Empty);
                slow_window.BorderWidth = 10;
                slow_window.WindowPosition = Gtk.WindowPosition.Center;
                slow_window.DeleteEvent += delegate (object o, Gtk.DeleteEventArgs args) {
                    args.RetVal = true;
                };
                
                Gtk.VBox box = new Gtk.VBox ();
                box.Spacing = 5;
                
                Gtk.Label title_label = new Gtk.Label ();
                title_label.Xalign = 0.0f;
                title_label.Markup = String.Format ("<b><big>{0}</big></b>",
                    GLib.Markup.EscapeText (title));
                
                Gtk.Label message_label = new Gtk.Label ();
                message_label.Xalign = 0.0f;
                message_label.Text = message;
                message_label.Wrap = true;
                
                slow_progress = new Gtk.ProgressBar ();
                
                box.PackStart (title_label, false, false, 0);
                box.PackStart (message_label, false, false, 0);
                box.PackStart (slow_progress, false, false, 0);
                
                slow_window.Add (box);
                slow_window.ShowAll ();
                
                IterateSlow ();
            }*/
        }
        
        private void OnMigrationSlowPulse (object o, EventArgs args)
        {
            /*lock (this) {
                slow_progress.Pulse ();
                IterateSlow ();
            }*/
        }
        
        private void OnMigrationSlowFinished (object o, EventArgs args)
        {
            /*lock (this) {
                slow_window.Destroy ();
                IterateSlow ();
            }*/
        }

        public override string DatabaseFile {
            get {
                if (ApplicationContext.CommandLine.Contains ("db"))
                    return ApplicationContext.CommandLine["db"];

                string dbfile = Path.Combine (Path.Combine (Environment.GetFolderPath (
                    Environment.SpecialFolder.ApplicationData), 
                    "banshee"), 
                    "banshee.db"); 

                if (!File.Exists (dbfile)) {
                    string tdbfile = Path.Combine (Path.Combine (Path.Combine (Environment.GetFolderPath (
                        Environment.SpecialFolder.Personal),
                        ".gnome2"),
                        "banshee"),
                        "banshee.db");

                    if (File.Exists (tdbfile)) {
                        dbfile = tdbfile;
                    }
                }

                return dbfile;
            }
        }
        
        string IService.ServiceName {
            get { return "DbConnection"; }
        }
    }
    
    public sealed class BansheeDbCommand : HyenaSqliteCommand
    {
        public BansheeDbCommand(string command)
            : base(command)
        {
        }

        public BansheeDbCommand (string command, int num_params)
            : base(command, num_params)
        {
        }

        public BansheeDbCommand (string command, params object [] param_values)
            : base(command, param_values.Length)
        {
        }
    }
}
