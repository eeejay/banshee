//
// InterfaceActionService.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using System.Reflection;
using System.Collections.Generic;

using Gtk;

using Banshee.Sources;
using Banshee.ServiceStack;

namespace Banshee.Gui
{
    public class InterfaceActionService : IService
    {
        private UIManager ui_manager = new UIManager ();    
        private Dictionary<string, ActionGroup> action_groups = new Dictionary<string, ActionGroup> ();

        private GlobalActions   global_actions;
        private ViewActions     view_actions;
        private PlaybackActions playback_actions;
        private TrackActions    track_actions;
        private SourceActions   source_actions;
        
        private BansheeActionGroup active_source_actions;
        private uint active_source_uiid = 0;
        
        public InterfaceActionService ()
        {
            global_actions      = new GlobalActions (this);
            view_actions        = new ViewActions (this);
            playback_actions    = new PlaybackActions (this);
            track_actions       = new TrackActions (this);
            source_actions      = new SourceActions (this);

            AddActionGroup (global_actions);
            AddActionGroup (view_actions);
            AddActionGroup (playback_actions);
            AddActionGroup (track_actions);
            AddActionGroup (source_actions);

            ui_manager.AddUiFromResource ("core-ui-actions-layout.xml");    
            
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
        }
        
        private void InnerAddActionGroup (ActionGroup group)
        {
            action_groups.Add (group.Name, group);
            ui_manager.InsertActionGroup (group, 0);
        }

        public void AddActionGroup (string name)
        {
            lock (this) {
                if (action_groups.ContainsKey (name)) {
                    throw new ApplicationException ("Group already exists");
                }
                
                InnerAddActionGroup (new ActionGroup (name));
            }
        }
        
        public void AddActionGroup (ActionGroup group)
        {
            lock (this) {
                if (action_groups.ContainsKey (group.Name)) {
                    throw new ApplicationException ("Group already exists");
                }
                            
                InnerAddActionGroup (group);
            }
        }
        
        public void RemoveActionGroup (string name)
        {
            lock (this) {
                if (action_groups.ContainsKey (name)) {
                    ActionGroup group = action_groups[name];
                    ui_manager.RemoveActionGroup (group);
                    action_groups.Remove (name);                    
                }
            }
        }
        
        public Action FindAction (string actionId)
        {
            string [] parts = actionId.Split ('.');
            
            if (parts == null || parts.Length < 2) {
                return null;
            }
            
            string group_name = parts[0];
            string action_name = parts[1];
            
            foreach (ActionGroup group in action_groups.Values) {
                if (group.Name != group_name) {
                    continue;
                }
                
                return group.GetAction (action_name);
            }
            
            return null;
        }
        
        public void PopulateToolbarPlaceholder (Toolbar toolbar, string path, Widget item)
        {
            PopulateToolbarPlaceholder (toolbar, path, item, false);
        }
        
        public void PopulateToolbarPlaceholder (Toolbar toolbar, string path, Widget item, bool expand)
        {
            ToolItem placeholder = (ToolItem)UIManager.GetWidget (path);
            int position = toolbar.GetItemIndex (placeholder);
            toolbar.Remove (placeholder);
            
            if (item is ToolItem) {
                ((ToolItem)item).Expand = expand;
                toolbar.Insert ((ToolItem)item, position);
            } else {
                ToolItem container_item = new Banshee.Widgets.GenericToolItem<Widget> (item);
                container_item.Expand = expand;
                container_item.Show ();
                toolbar.Insert (container_item, position);
            }
        }
        
        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            if (active_source_uiid > 0) {
                ui_manager.RemoveUi (active_source_uiid);
                active_source_uiid = 0;
            }
                
            if (active_source_actions != null) {
                RemoveActionGroup (active_source_actions.Name);
                active_source_actions = null;
            }
            
            Source active_source = ServiceManager.SourceManager.ActiveSource;
            if (active_source == null) {
                return;
            }
            
            active_source_actions = active_source.Properties.Get<BansheeActionGroup> ("ActiveSourceActions");
            if (active_source_actions != null) {
                AddActionGroup (active_source_actions);
            }
                
            Assembly assembly = Assembly.GetAssembly (active_source.GetType ());
            string ui_file = active_source.Properties.GetString ("ActiveSourceUIResource");
            
            if (ui_file != null) {
                using (StreamReader reader = new StreamReader (assembly.GetManifestResourceStream (ui_file))) {
                    active_source_uiid = ui_manager.AddUiFromString (reader.ReadToEnd ());
                }
            }
        }
        
        public Action this[string actionId] {
            get { return FindAction (actionId); }
        }
        
        public UIManager UIManager {
            get { return ui_manager; }
        }
        
        public GlobalActions GlobalActions {
            get { return global_actions; }
        }
        
        public PlaybackActions PlaybackActions {
            get { return playback_actions; }
        }

        public TrackActions TrackActions {
            get { return track_actions; }
        }

        public SourceActions SourceActions {
            get { return source_actions; }
        }
        
        string IService.ServiceName {
            get { return "InterfaceActionService"; }
        }
    }
}
