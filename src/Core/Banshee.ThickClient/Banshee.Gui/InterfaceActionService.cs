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
using System.Collections.Generic;

using Gtk;

using Banshee.ServiceStack;

namespace Banshee.Gui
{
    public class InterfaceActionService : IService
    {
        private UIManager ui_manager = new UIManager ();    
        private Dictionary<string, ActionGroup> action_groups = new Dictionary<string, ActionGroup> ();
        
        public InterfaceActionService ()
        {
            AddActionGroup (new GlobalActions (this));
            AddActionGroup (new PlaybackActions (this));
            
            ui_manager.AddUiFromResource ("core-ui-actions-layout.xml");            
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
        
        public Action this[string actionId] {
            get { return FindAction (actionId); }
        }
        
        public UIManager UIManager {
            get { return ui_manager; }
        }
        
        public ActionGroup GlobalActions {
            get { return action_groups["Global"]; }
        }
        
        public PlaybackActions PlaybackActions {
            get { return (PlaybackActions)action_groups["Playback"]; }
        }
        
        string IService.ServiceName {
            get { return "InterfaceActionService"; }
        }
    }
}
