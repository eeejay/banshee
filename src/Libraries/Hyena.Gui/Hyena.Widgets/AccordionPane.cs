//
// AccordionPane.cs
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

namespace Hyena.Widgets
{
    public class AccordionPane : Bin
    {
        private List<Gtk.Paned> panes = new List<Paned> ();
        
        private Box 
        
        public AccordionPane () : base ()
        {
        }
    }
    
#region Test Module

    [Hyena.Gui.TestModule ("Accordion Pane")]
    internal class RatingEntryTestModule : Gtk.Window
    {
        public RatingEntryTestModule () : base ("Rating Entry")
        {
            VBox pbox = new VBox ();
            Add (pbox);
            
            Menu m = new Menu ();
            MenuBar b = new MenuBar ();
            MenuItem item = new MenuItem ("Rate Me!");
            item.Submenu = m;
            b.Append (item);
            m.Append (new MenuItem ("Apples"));
            m.Append (new MenuItem ("Pears"));
            m.Append (new RatingMenuItem ());
            m.Append (new ImageMenuItem ("gtk-remove", null));
            m.ShowAll ();
            pbox.PackStart (b, false, false, 0);
            
            VBox box = new VBox ();
            box.BorderWidth = 10;
            box.Spacing = 10;
            pbox.PackStart (box, true, true, 0);
            
            RatingEntry entry1 = new RatingEntry ();
            box.PackStart (entry1, true, true, 0);
            
            RatingEntry entry2 = new RatingEntry ();
            box.PackStart (entry2, false, false, 0);
            
            box.PackStart (new Entry ("Normal GtkEntry"), false, false, 0);
            
            RatingEntry entry3 = new RatingEntry ();
            Pango.FontDescription fd = entry3.PangoContext.FontDescription.Copy ();
            fd.Size = (int)(fd.Size * Pango.Scale.XXLarge);
            entry3.ModifyFont (fd);
            box.PackStart (entry3, true, true, 0);
            
            pbox.ShowAll ();
        }
    }
    
#endregion
}