//
// SmartPlaylistQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections.Generic;
using Gtk;

using Hyena.Query;
using Hyena.Query.Gui;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Playlist;
using Banshee.SmartPlaylist;
using Banshee.Widgets;
using Banshee.Query;

namespace Banshee.Query.Gui
{
    public class SmartPlaylistQueryValueEntry : QueryValueEntry
    {
        protected ComboBox combo;
        protected SmartPlaylistQueryValue query_value;
        protected Dictionary<int, int> playlist_id_combo_map = new Dictionary<int, int> ();
        protected Dictionary<int, int> combo_playlist_id_map = new Dictionary<int, int> ();

        public SmartPlaylistQueryValueEntry () : base ()
        {
            combo = ComboBox.NewText();
            combo.WidthRequest = DefaultWidth;

            int count = 0;
            SmartPlaylistSource playlist;
            foreach (Source child in ServiceManager.SourceManager.DefaultSource.Children) {
                playlist = child as SmartPlaylistSource;
                if (playlist != null && playlist.DbId != null) {
                    if (Editor.CurrentlyEditing == null || (Editor.CurrentlyEditing != playlist && !playlist.DependsOn (Editor.CurrentlyEditing))) {
                        combo.AppendText (playlist.Name);
                        playlist_id_combo_map [(int)playlist.DbId] = count;
                        combo_playlist_id_map [count++] = (int) playlist.DbId;
                    }
                }
            }

            Add (combo);
            combo.Active = 0;

            combo.Changed += HandleValueChanged;
        }

        public override QueryValue QueryValue {
            get { return query_value; }
            set {
                combo.Changed -= HandleValueChanged;
                query_value = value as SmartPlaylistQueryValue;
                if (!query_value.IsEmpty) {
                    try {
                        combo.Active = playlist_id_combo_map [(int)query_value.IntValue];
                    } catch {}
                }


                HandleValueChanged (null, EventArgs.Empty);
                combo.Changed += HandleValueChanged;
            }
        }

        protected void HandleValueChanged (object o, EventArgs args)
        {
            if (combo_playlist_id_map.ContainsKey (combo.Active)) {
                query_value.SetValue (combo_playlist_id_map [combo.Active]);
            }
        }
    }
}
