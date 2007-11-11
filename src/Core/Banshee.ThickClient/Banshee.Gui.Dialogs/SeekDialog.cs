//
// SeekDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections;
using Mono.Unix;
using Gtk;
using Glade;

using Banshee.Base;
using Banshee.Gui.Widgets;

namespace Banshee.Gui.Dialogs
{
    public class SeekDialog : GladeDialog
    {
        [Widget] private VBox seek_box;
        private ConnectedSeekSlider seek_slider;
        
        public SeekDialog () : base ("SeekDialog")
        {
            seek_slider = new ConnectedSeekSlider ();
            seek_slider.StreamPositionLabel.FormatString = "<big>{0}</big>";
            
            seek_box.PackStart (seek_slider, false, false, 0);
            seek_box.ShowAll ();
            
            Dialog.SetSizeRequest (300, -1);
        }
    }
}
