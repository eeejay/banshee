//
// RangeEntry.cs
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

namespace Banshee.Gui.TrackEditor
{
    public class RangeEntry : HBox, IEditorField
    {
        public event EventHandler Changed;
    
        private SpinButton from_entry;
        public SpinButton From {
            get { return from_entry; }
        }
        
        private SpinButton to_entry;
        public SpinButton To {
            get { return to_entry; }
        }
    
        public RangeEntry (string rangeLabel)
        {
            PackStart (from_entry = new SpinButton (0, 99, 1), true, true, 0);
            PackStart (new Label (rangeLabel), false, false, 6);
            PackStart (to_entry = new SpinButton (0, 99, 1), true, true, 0);
            ShowAll ();
            
            from_entry.WidthChars = 2;
            to_entry.WidthChars = 2;
            
            from_entry.ValueChanged += OnChanged;
            to_entry.ValueChanged += OnChanged;
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
