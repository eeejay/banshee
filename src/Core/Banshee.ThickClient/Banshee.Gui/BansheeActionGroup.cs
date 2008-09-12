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
        private InterfaceActionService action_service;
        private Dictionary<string, string> labels = new Dictionary<string, string> ();
        private Dictionary<string, string> icons = new Dictionary<string, string> ();
        private List<uint> ui_merge_ids = new List<uint> ();

        private bool important_by_default = true;
        protected bool ImportantByDefault {
            get { return important_by_default; }
            set { important_by_default = value; }
        }

        public BansheeActionGroup (string name)
            : this (ServiceManager.Get<InterfaceActionService> (), name)
        {
        }
        
        public BansheeActionGroup (InterfaceActionService action_service, string name) : base (name)
        {
            this.action_service = action_service;
        }

        public void AddUiFromFile (string ui_file)
        {
            Banshee.Base.ThreadAssist.AssertInMainThread ();
            ui_merge_ids.Add (Actions.AddUiFromFile (ui_file, System.Reflection.Assembly.GetCallingAssembly ()));
        }

        public void Register ()
        {
            if (Actions.FindActionGroup (this.Name) == null) {
                Actions.AddActionGroup (this);
            }
        }

        public void UnRegister ()
        {
            if (Actions.FindActionGroup (this.Name) != null) {
                Actions.RemoveActionGroup (this);
            }
        }

        public override void Dispose ()
        {
            Banshee.Base.ThreadAssist.ProxyToMain (delegate {
                UnRegister ();

                foreach (uint merge_id in ui_merge_ids) {
                    if (merge_id > 0) {
                        Actions.UIManager.RemoveUi (merge_id);
                    }
                }
                ui_merge_ids.Clear ();

                base.Dispose ();
            });
        }

        public new void Add (ActionEntry [] action_entries)
        {
            if (ImportantByDefault) {
                AddImportant (action_entries);
            } else {
                base.Add (action_entries);
            }
        }
        
        public void AddImportant (params ActionEntry [] action_entries)
        {
            base.Add (action_entries);
            
            foreach (ActionEntry entry in action_entries) {
                this[entry.name].IsImportant = true;
            }
        }

        public void AddImportant (params ToggleActionEntry [] action_entries)
        {
            base.Add (action_entries);
            
            foreach (ToggleActionEntry entry in action_entries) {
                this[entry.name].IsImportant = true;
            }
        }
        
        public void Remove (string actionName)
        {
            Gtk.Action action = this[actionName];
            if (action != null) {
                Remove (action);
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

        public void UpdateAction (string action_name, bool visible_and_sensitive)
        {
            UpdateAction (action_name, visible_and_sensitive, visible_and_sensitive);
        }
        
        public void UpdateAction (string action_name, bool visible, bool sensitive)
        {
            UpdateAction (action_name, visible, sensitive, null);
        }

        public void UpdateAction (string action_name, bool visible, bool sensitive, Source source)
        {
            Gtk.Action action = this[action_name];
            UpdateAction (action, visible, sensitive);

            if (source != null && action.Visible) {
                // Save the original label
                if (!labels.ContainsKey (action_name))
                    labels.Add (action_name, action.Label);

                // Save the original icon name
                if (!icons.ContainsKey (action_name))
                    icons.Add (action_name, action.IconName);

                // If this source has a label property for this action, override the current label, otherwise reset it
                // to the original label
                string label = source.Properties.Get<string> (String.Format ("{0}Label", action_name)) ?? labels[action_name];
                action.Label = label;

                // If this source has an icon property for this action, override the current icon, othewise reset it
                // to the original icon
                string icon = source.Properties.Get<string> (String.Format ("{0}IconName", action_name)) ?? icons[action_name];
                if (!String.IsNullOrEmpty (icon)) {
                    action.IconName = icon;
                }
            }
        }
        
        public static void UpdateAction (Gtk.Action action, bool visible_and_sensitive)
        {
            UpdateAction (action, visible_and_sensitive, visible_and_sensitive);
        }
        
        public static void UpdateAction (Gtk.Action action, bool visible, bool sensitive)
        {
            action.Visible = visible;
            action.Sensitive = visible && sensitive;
        }
        
        protected void ShowContextMenu (string menu_name)
        {
            Gtk.Menu menu = Actions.UIManager.GetWidget (menu_name) as Menu;
            if (menu == null || menu.Children.Length == 0) {
                return;
            }

            int visible_children = 0;
            foreach (Widget child in menu)
                if (child.Visible)
                    visible_children++;

            if (visible_children == 0) {
                return;
            }

            menu.Show (); 
            menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
        }
        
        public InterfaceActionService Actions {
            get { return action_service; }
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
