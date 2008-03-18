//
// BansheeActionGroup.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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

using Gtk;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;

namespace Banshee.Gui
{
    public class BansheeActionGroup : ActionGroup
    {
        protected Dictionary<string, string> labels = new Dictionary<string, string> ();

        public BansheeActionGroup (string name) : base (name)
        {
        }
        
        public void AddImportant (params ActionEntry [] action_entries)
        {
            base.Add (action_entries);
            
            foreach (ActionEntry entry in action_entries) {
                this[entry.name].IsImportant = true;
            }
        }

        public void UpdateActions (bool visible, bool sensitive, params string [] action_names)
        {
            UpdateActions (visible, sensitive, null, action_names);
        }

        public void UpdateActions (bool visible, bool sensitive, Source source, params string [] action_names)
        {
            foreach (string name in action_names) {
                UpdateAction (name, visible, sensitive, source);
            }
        }

        public void UpdateAction (string action_name, bool visible, bool sensitive)
        {
            UpdateAction (action_name, visible, sensitive, null);
        }

        public void UpdateAction (string action_name, bool visible, bool sensitive, Source source)
        {
            Gtk.Action action = this[action_name];
            action.Visible = visible;
            action.Sensitive = visible && sensitive;

            if (source != null && action.Visible) {
                if (!labels.ContainsKey (action_name)) {
                    labels.Add (action_name, action.Label);
                    //Console.WriteLine ("action label {0} for {1} just not saved", action.Label, action_name);
                }
                string label = source.Properties.GetString (String.Format ("{0}Label", action_name));
                action.Label = (label == null || label == String.Empty) ? labels[action_name] : label;
                //Console.WriteLine ("for source {0} and action {1} got label {2}, so set action.Label = {3}", source.Name, action_name, label, action.Label);
            }
        }

        public Source ActiveSource {
            get { return ServiceManager.SourceManager.ActiveSource; }
        }

        public virtual PrimarySource ActivePrimarySource {
            get { return (ActiveSource as PrimarySource) ?? (ActiveSource.Parent as PrimarySource) ?? ServiceManager.SourceManager.MusicLibrary; }
        }

        public Gtk.Window PrimaryWindow {
            get { return ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow; }
        }
    }
}
