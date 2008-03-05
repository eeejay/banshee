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
using Banshee.Configuration;

namespace Banshee.Gui
{
    public class PlaybackRepeatActions : BansheeActionGroup, IEnumerable<RadioAction>
    {
        private RadioAction active_action;

        public RadioAction Active {
            get { return active_action; }
            set {
                active_action = value;
                RepeatMode.Set (active_action == null ? String.Empty : ActionNameToConfigId (active_action.Name));
            }
        }

        public event ChangedHandler Changed;
        
        public PlaybackRepeatActions (InterfaceActionService actionService) : base ("PlaybackRepeat")
        {
            actionService.AddActionGroup (this);

            Add (new RadioActionEntry [] {
                new RadioActionEntry ("RepeatNoneAction", "media-repeat-none", 
                    Catalog.GetString ("Repeat N_one"), null,
                    Catalog.GetString ("Do not repeat playlist"), 0),
                    
                new RadioActionEntry ("RepeatAllAction", "media-repeat-all",
                    Catalog.GetString ("Repeat _All"), null,
                    Catalog.GetString ("Play all songs before repeating playlist"), 1),
                    
                new RadioActionEntry ("RepeatSingleAction", "media-repeat-single",
                    Catalog.GetString ("Repeat Si_ngle"), null,
                    Catalog.GetString ("Repeat the current playing song"), 2)
            }, 0, OnChanged);

            Gtk.Action action = this[ConfigIdToActionName (RepeatMode.Get ())];
            if (action is RadioAction) {
                active_action = (RadioAction)action;
            } else {
                Active = (RadioAction)this["RepeatNoneAction"];
            }
            Active.Activate ();
        }

        private void OnChanged (object o, ChangedArgs args)
        {
            Active = args.Current;
            
            ChangedHandler handler = Changed;
            if (handler != null) {
                handler (o, args);
            }
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
