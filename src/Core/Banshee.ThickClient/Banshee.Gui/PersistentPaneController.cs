//
// PersistentPanedController.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (c) 2008 Scott Peterson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gtk;

using Banshee.Configuration;

namespace Banshee.Gui
{
    public class PersistentPaneController
    {
        private static Dictionary<string, PersistentPaneController> controllers = new Dictionary<string, PersistentPaneController> ();

        private string @namespace;
        private string key;
        private int fallback;
        private uint timer_id = 0;
        private bool pending_changes;
        private Paned pane;
        private int last_position;

        public static void Control (Paned pane, string name)
        {
            Control (pane, String.Format ("interface.panes.{0}", name), "position", pane.Position);
        }

        public static void Control (Paned pane, SchemaEntry<int> entry)
        {
            Control (pane, entry.Namespace, entry.Key, entry.DefaultValue);
        }

        private static void Control (Paned pane, string @namespace, string key, int defaultValue)
        {
            string dict_key = String.Format ("{0}.{1}", @namespace, key);
            if (controllers.ContainsKey (dict_key)) {
                controllers[dict_key].Paned = pane;
            } else {
                controllers.Add (dict_key, new PersistentPaneController (pane, @namespace, key, defaultValue));
            }
        }

        private PersistentPaneController (Paned pane, string @namespace, string key, int fallback)
        {
            this.@namespace = @namespace;
            this.key = key;
            this.fallback = fallback;
            Paned = pane;
        }

        private Paned Paned {
            set {
                if (pane == value) {
                    return;
                }

                if (pane != null) {
                    //pane.MoveHandle -= OnPaneMoved;
                }

                pane = value;
                pane.Position = ConfigurationClient.Get<int> (@namespace, key, fallback);
                //pane.MoveHandle += OnPaneMoved;
                //pane.AcceptPosition += delegate { Console.WriteLine ("accept pos called, pos = {0}", pane.Position); };
                pane.SizeAllocated += OnPaneMoved;
            }
        }

        private void OnPaneMoved (object sender, EventArgs args)
        {
            Save ();
        }

        private void Save ()
        {
            if (timer_id == 0) {
                timer_id = GLib.Timeout.Add (500, OnTimeout);
            } else {
                pending_changes = true;
            }
        }

        private bool OnTimeout ()
        {
            if (pending_changes) {
                pending_changes = false;
                return true;
            } else {
                if (pane.Position != last_position) {
                    ConfigurationClient.Set<int> (@namespace, key, pane.Position);
                    last_position = pane.Position;
                }
                timer_id = 0;
                return false;
            }
        }
    }
}