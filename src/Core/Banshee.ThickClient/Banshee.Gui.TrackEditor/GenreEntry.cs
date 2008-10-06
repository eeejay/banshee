//
// GenreEntry.cs
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

using Hyena.Gui;

using Banshee.ServiceStack;
using Banshee.Collection.Database;

namespace Banshee.Gui.TrackEditor
{
    public class GenreEntry : ComboBoxEntry, ICanUndo, IEditorField
    {
        private ListStore genre_model;
        private EditorEntryUndoAdapter undo_adapter = new EditorEntryUndoAdapter ();
        
        public GenreEntry ()
        {
            genre_model = new ListStore (typeof (string));
            Model = genre_model;
            TextColumn = 0;

            EntryCompletion c = new EntryCompletion ();
            c.Model = genre_model;
            c.TextColumn = TextColumn;
            c.PopupCompletion = true;
            c.InlineCompletion = true;
            //c.InlineSelection = true; // requires 2.12
            c.PopupSingleMatch = false;
            Entry.Completion = c;
        
            foreach (string genre in ServiceManager.DbConnection.QueryEnumerable<string> (
                "SELECT DISTINCT Genre FROM CoreTracks ORDER BY Genre")) {
                if (!String.IsNullOrEmpty (genre)) {
                    genre_model.AppendValues (genre);
                }
            }
        }
        
        public void DisconnectUndo ()
        {
            undo_adapter.DisconnectUndo ();
        }
        
        public void ConnectUndo (EditorTrackInfo track)
        {
            undo_adapter.ConnectUndo (Entry, track);
        }
        
        public string Value {
            get { return Entry.Text; }
            set { Entry.Text = value ?? String.Empty; }
        }
    }
}
