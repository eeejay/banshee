//
// TextViewEntry.cs
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

using Hyena.Widgets;

namespace Banshee.Gui.TrackEditor
{
    public class TextViewEntry : Gtk.ScrolledWindow, IEditorField, ICanUndo
    {    
        private EditorEditableUndoAdapter<TextViewEditable> undo_adapter 
            = new EditorEditableUndoAdapter<TextViewEditable> ();
        
        public event EventHandler Changed;
        
        private TextViewEditable entry;
        public TextView TextView {
            get { return entry; }
        }
        
        public string Text {
            get { return entry.Buffer.Text; }
            set { entry.Buffer.Text = value ?? String.Empty; }
        }

        public TextViewEntry ()
        {
            VscrollbarPolicy = PolicyType.Automatic;
            HscrollbarPolicy = PolicyType.Never;
            ShadowType = ShadowType.In;
            
            Add (entry = new TextViewEditable ());
            entry.AcceptsTab = false;
            entry.Show ();
            entry.Buffer.Changed += OnChanged;
            
            entry.SizeRequested += OnTextViewSizeRequested;
        }
        
        private void OnTextViewSizeRequested (object o, SizeRequestedArgs args)
        {
            Pango.FontMetrics metrics = PangoContext.GetMetrics (entry.Style.FontDescription, PangoContext.Language);
            int line_height = ((int)(metrics.Ascent + metrics.Descent) + 512) >> 10; // PANGO_PIXELS(d)
            metrics.Dispose ();
            HeightRequest = (line_height + 2) * 2;
        }
        
        public void DisconnectUndo ()
        {
            undo_adapter.DisconnectUndo ();
        }
        
        public void ConnectUndo (EditorTrackInfo track)
        {
            undo_adapter.ConnectUndo (entry, track);
        }
                
        private void OnChanged (object o, EventArgs args)
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}
            