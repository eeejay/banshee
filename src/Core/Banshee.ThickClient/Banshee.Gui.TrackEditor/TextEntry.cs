//
// TextEntry.cs
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

using Banshee.ServiceStack;

namespace Banshee.Gui.TrackEditor
{
    public class TextEntry : Entry, IEditorField, ICanUndo
    {
        private EditorEntryUndoAdapter undo_adapter = new EditorEntryUndoAdapter ();

        public TextEntry () : base ()
        {
        }

        public TextEntry (string completion_table, string completion_column) : this ()
        {
            ListStore completion_model = new ListStore (typeof (string));
            foreach (string val in ServiceManager.DbConnection.QueryEnumerable<string> (String.Format (
                "SELECT DISTINCT {1} FROM {0} ORDER BY {1}", completion_table, completion_column))) {
                if (!String.IsNullOrEmpty (val)) {
                    completion_model.AppendValues (val);
                }
            }

            Completion = new EntryCompletion ();
            Completion.Model = completion_model;
            Completion.TextColumn = 0;
            Completion.PopupCompletion = true;
            Completion.InlineCompletion = true;
            //Completion.InlineSelection = true; // requires 2.12
            Completion.PopupSingleMatch = false;
        }
        
        public void DisconnectUndo ()
        {
            undo_adapter.DisconnectUndo ();
        }
        
        public void ConnectUndo (EditorTrackInfo track)
        {
            undo_adapter.ConnectUndo (this, track);
        }
        
        public new string Text {
            get { return base.Text; }
            set { base.Text = value ?? String.Empty; }
        }
    }
}
