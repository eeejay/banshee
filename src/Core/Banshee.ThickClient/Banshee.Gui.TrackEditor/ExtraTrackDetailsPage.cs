//
// ExtraTrackDetailsPage.cs
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
using Mono.Unix;
using Gtk;

using Banshee.Collection;

namespace Banshee.Gui.TrackEditor
{
    public class ExtraTrackDetailsPage : FieldPage, ITrackEditorPage
    {        
        public int Order {
            get { return 20; }
        }
                                    
        public string Title {
            get { return Catalog.GetString ("Extra"); }
        }
        
        protected override void AddFields ()
        {
            AddField (this, new TextEntry (), 
                Catalog.GetString ("Set all composers to this value"),
                delegate { return Catalog.GetString ("Composer:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.Composer; },
                delegate (EditorTrackInfo track, Widget widget) {  track.Composer = ((TextEntry)widget).Text; }
            );
            
            AddField (this, new TextEntry (), 
                Catalog.GetString ("Set all conductors to this value"),
                delegate { return Catalog.GetString ("Conductor:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.Conductor; },
                delegate (EditorTrackInfo track, Widget widget) { track.Conductor = ((TextEntry)widget).Text; }
            );
            
            HBox box = new HBox ();
            box.Spacing = 12;
            box.Show ();
            PackStart (box, false, false, 0);
            
            AddField (box, new TextEntry (), 
                Catalog.GetString ("Set all groupings to this value"),
                delegate { return Catalog.GetString ("Grouping:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.Grouping; },
                delegate (EditorTrackInfo track, Widget widget) { track.Grouping = ((TextEntry)widget).Text; }
            );
            
            AddField (box, new SpinButtonEntry (0, 500, 1), 
                Catalog.GetString ("Set all beats per minute to this value"),
                delegate { return Catalog.GetString ("Beats Per Minute:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((SpinButtonEntry)widget).Value = track.Bpm; },
                delegate (EditorTrackInfo track, Widget widget) { track.Bpm = (int)((SpinButtonEntry)widget).Value; },
                FieldOptions.Shrink
            );
            
            AddField (this, new TextEntry (), 
                Catalog.GetString ("Set all copyrights to this value"),
                delegate { return Catalog.GetString ("Copyright:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.Copyright; },
                delegate (EditorTrackInfo track, Widget widget) { track.Copyright = ((TextEntry)widget).Text; }
            );
            
            AddField (this, new TextViewEntry (), 
                Catalog.GetString ("Set all comments to this value"),
                delegate { return Catalog.GetString ("Comment:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextViewEntry)widget).Text = track.Comment; },
                delegate (EditorTrackInfo track, Widget widget) { track.Comment = ((TextViewEntry)widget).Text; }
            );
        }
    }
}
