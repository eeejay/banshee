//
// PlaybackRepeatActions.cs
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
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Gui;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.PlaybackController;

namespace Banshee.Gui
{
    public class PlaybackRepeatActions : BansheeActionGroup, IEnumerable<RadioAction>
    {
        private InterfaceActionService action_service;
        private RadioAction active_action;
        
        public RadioAction Active {
            get { return active_action; }
            set {
                active_action = value;
                RepeatMode.Set (active_action == null ? String.Empty : ActionNameToConfigId (active_action.Name));
                ServiceManager.PlaybackController.RepeatMode = (PlaybackRepeatMode)active_action.Value;
                OnChanged ();
            }
        }
        
        public new bool Sensitive {
            get { return base.Sensitive; }
            set {
                base.Sensitive = value;
                OnChanged ();
            }
        }

        public event EventHandler Changed;
        
        public PlaybackRepeatActions (InterfaceActionService actionService) : base ("PlaybackRepeat")
        {
            action_service = actionService;
            actionService.AddActionGroup (this);
            
            Add (new ActionEntry [] {
                new ActionEntry ("RepeatMenuAction", null,
                    Catalog.GetString ("Repeat"), null,
                    Catalog.GetString ("Repeat"), null)
            });

            Add (new RadioActionEntry [] {
                new RadioActionEntry ("RepeatNoneAction", null, 
                    Catalog.GetString ("Repeat _Off"), null,
                    Catalog.GetString ("Do not repeat playlist"),
                    (int)PlaybackRepeatMode.None),
                    
                new RadioActionEntry ("RepeatAllAction", null,
                    Catalog.GetString ("Repeat _All"), null,
                    Catalog.GetString ("Play all songs before repeating playlist"),
                    (int)PlaybackRepeatMode.RepeatAll),
                    
                new RadioActionEntry ("RepeatSingleAction", null,
                    Catalog.GetString ("Repeat Singl_e"), null,
                    Catalog.GetString ("Repeat the current playing song"),
                    (int)PlaybackRepeatMode.RepeatSingle)
            }, 0, OnActionChanged);

            this["RepeatNoneAction"].IconName = "media-repeat-none";
            this["RepeatAllAction"].IconName = "media-repeat-all";
            this["RepeatSingleAction"].IconName = "media-repeat-single";

            Gtk.Action action = this[ConfigIdToActionName (RepeatMode.Get ())];
            if (action is RadioAction) {
                active_action = (RadioAction)action;
            } else {
                Active = (RadioAction)this["RepeatNoneAction"];
            }
            
            Active.Activate ();
        }

        private void OnActionChanged (object o, ChangedArgs args)
        {
            Active = args.Current;
        }
        
        private void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public void AttachSubmenu (string menuItemPath)
        {
            MenuItem parent = action_service.UIManager.GetWidget (menuItemPath) as MenuItem;
            parent.Submenu = CreateMenu ();
        }
        
        public MenuItem CreateSubmenu ()
        {
            MenuItem parent = (MenuItem)this["RepeatMenuAction"].CreateMenuItem ();
            parent.Submenu = CreateMenu ();
            return parent;
        }
            
        public Menu CreateMenu ()
        {
            Menu menu = new Gtk.Menu ();
            bool separator = false;
            foreach (RadioAction action in this) {
                menu.Append (action.CreateMenuItem ());
                if (!separator) {
                    separator = true;
                    menu.Append (new SeparatorMenuItem ());
                }
            }
            menu.ShowAll ();
            return menu;
        }

        public IEnumerator<RadioAction> GetEnumerator ()
        {
            yield return (RadioAction)this["RepeatNoneAction"];
            yield return (RadioAction)this["RepeatAllAction"];
            yield return (RadioAction)this["RepeatSingleAction"];
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        private static string ConfigIdToActionName (string configuration)
        {
            return String.Format ("{0}Action", StringUtil.UnderCaseToCamelCase (configuration));
        }

        private static string ActionNameToConfigId (string actionName)
        {
            return StringUtil.CamelCaseToUnderCase (actionName.Substring (0, 
                actionName.Length - (actionName.EndsWith ("Action") ? 6 : 0)));
        }

        public static readonly SchemaEntry<string> RepeatMode = new SchemaEntry<string> (
            "playback", "repeat_mode",
            "none",
            "Repeat playback",
            "Repeat mode (repeat_none, repeat_all, repeat_single)"
        );
    }
}
