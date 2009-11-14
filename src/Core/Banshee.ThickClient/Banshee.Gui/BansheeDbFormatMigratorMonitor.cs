//
// BansheeDbFormatMigratorMonitor.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Gtk;

using Banshee.Database;

namespace Banshee.Gui
{
    public class BansheeDbFormatMigratorMonitor
    {
        private Gtk.Window slow_window;
        private Gtk.ProgressBar slow_progress;

        private BansheeDbFormatMigrator migrator;

        public BansheeDbFormatMigratorMonitor (BansheeDbFormatMigrator migrator)
        {
            if (migrator == null) {
                return;
            }

            this.migrator = migrator;
            migrator.Finished += OnMigrationFinished;
            migrator.SlowStarted += OnMigrationSlowStarted;
            migrator.SlowPulse += OnMigrationSlowPulse;
            migrator.SlowFinished += OnMigrationSlowFinished;
        }

        private void OnMigrationFinished (object o, EventArgs args)
        {
            migrator.Finished -= OnMigrationFinished;
            migrator.SlowStarted -= OnMigrationSlowStarted;
            migrator.SlowPulse -= OnMigrationSlowPulse;
            migrator.SlowFinished -= OnMigrationSlowFinished;
            migrator = null;
        }

        private void IterateSlow ()
        {
            while (Gtk.Application.EventsPending ()) {
                Gtk.Application.RunIteration ();
            }
        }

        private void OnMigrationSlowStarted (string title, string message)
        {
            lock (this) {
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
            }
        }

        private void OnMigrationSlowPulse (object o, EventArgs args)
        {
            lock (this) {
                slow_progress.Pulse ();
                IterateSlow ();
            }
        }

        private void OnMigrationSlowFinished (object o, EventArgs args)
        {
            lock (this) {
                slow_window.Destroy ();
                IterateSlow ();
            }
        }
    }
}
