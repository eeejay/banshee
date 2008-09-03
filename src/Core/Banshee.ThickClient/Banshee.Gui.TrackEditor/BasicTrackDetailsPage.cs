//
// BasicTrackDetailsPage.cs
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
    public class BasicTrackDetailsPage : FieldPage, ITrackEditorPage
    {
        private CheckButton enable_compilation = new CheckButton ();
        
        public int Order {
            get { return 10; }
        }
                                    
        public string Title {
            get { return Catalog.GetString ("Basic Details"); }
        }
        
        public override void LoadTrack (EditorTrackInfo track)
        {
            base.LoadTrack (track);
            enable_compilation.Active = track.IsCompilation;
            OnEnableCompilationToggled (enable_compilation, EventArgs.Empty);
        }

        protected override void AddFields ()
        {
            HBox box = new HBox ();
            VBox left = EditorUtilities.CreateVBox ();
            VBox right = EditorUtilities.CreateVBox ();
            
            box.PackStart (left, true, true, 0);
            box.PackStart (new VSeparator (), false, false, 12);
            box.PackStart (right, false, false, 0);
            box.ShowAll ();
            
            PackStart (box, false, false, 0);
            
            // Left
            
            AddField (left, new TitleEntry (Dialog), 
                delegate { return Catalog.GetString ("Track Title:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TitleEntry)widget).Text = track.TrackTitle; },
                delegate (EditorTrackInfo track, Widget widget) { track.TrackTitle = ((TitleEntry)widget).Text; },
                FieldOptions.NoSync
            );
            
            AddField (left, new TextEntry (), 
                delegate { return Catalog.GetString ("Track Artist:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.ArtistName; },
                delegate (EditorTrackInfo track, Widget widget) { track.ArtistName = ((TextEntry)widget).Text; }
            );
            
            enable_compilation.Toggled += OnEnableCompilationToggled;
            AddField (left, enable_compilation, new TextEntry (),
                delegate (EditorTrackInfo track, Widget widget) { 
                    ((CheckButton)widget).Label = Catalog.GetString ("Album Artist (part of a compilation):"); 
                    return null; 
                },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.AlbumArtist; },
                delegate (EditorTrackInfo track, Widget widget) { track.AlbumArtist = ((TextEntry)widget).Text; }
            );
            
            AddField (left, new TextEntry (), 
                delegate { return Catalog.GetString ("Album Title:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.AlbumTitle; },
                delegate (EditorTrackInfo track, Widget widget) { track.AlbumTitle = ((TextEntry)widget).Text; }
            );
            
            AddField (left, new GenreEntry (), 
                delegate { return Catalog.GetString ("Genre:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((GenreEntry)widget).Value = track.Genre; },
                delegate (EditorTrackInfo track, Widget widget) { track.Genre = ((GenreEntry)widget).Value; }
            );
            
            // Right
            
            AddField (right, new RangeEntry (Catalog.GetString ("of")), 
                delegate { return Catalog.GetString ("Track Number:"); },
                delegate (EditorTrackInfo track, Widget widget) {
                    RangeEntry entry = (RangeEntry)widget;
                    entry.From.Value = track.TrackNumber;
                    entry.To.Value = track.TrackCount;
                },
                delegate (EditorTrackInfo track, Widget widget) {
                    RangeEntry entry = (RangeEntry)widget;
                    track.TrackNumber = (int)entry.From.Value;
                    track.TrackCount = (int)entry.To.Value;
                },
                FieldOptions.NoSync
            );
            
            AddField (right, new RangeEntry (Catalog.GetString ("of")), 
                delegate { return Catalog.GetString ("Disc Number:"); },
                delegate (EditorTrackInfo track, Widget widget) {
                    RangeEntry entry = (RangeEntry)widget;
                    entry.From.Value = track.DiscNumber;
                    entry.To.Value = track.DiscCount;
                },
                delegate (EditorTrackInfo track, Widget widget) {
                    RangeEntry entry = (RangeEntry)widget;
                    track.DiscNumber = (int)entry.From.Value;
                    track.DiscCount = (int)entry.To.Value;
                },
                FieldOptions.NoSync
            );
            
            Label year_label = EditorUtilities.CreateLabel (null);
            enable_compilation.SizeAllocated += delegate { year_label.HeightRequest = enable_compilation.Allocation.Height; };
            AddField (right, year_label, new SpinButtonEntry (1000, 3000, 1),
                delegate { return Catalog.GetString ("Year:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((SpinButtonEntry)widget).Value = track.Year; },
                delegate (EditorTrackInfo track, Widget widget) { track.Year = (int)((SpinButtonEntry)widget).Value; },
                FieldOptions.Shrink
            );
            
            AddField (right, new RatingEntry (), 
                delegate { return Catalog.GetString ("Rating:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((RatingEntry)widget).Value = track.Rating; },
                delegate (EditorTrackInfo track, Widget widget) { track.Rating = ((RatingEntry)widget).Value; },
                FieldOptions.Shrink
            );
        }
        
        private void OnEnableCompilationToggled (object o, EventArgs args)
        {
            CheckButton check = (CheckButton)o;
            
            if (CurrentTrack != null) {
                CurrentTrack.IsCompilation = check.Active;
            }
            
            foreach (Widget child in ((Container)check.Parent).Children) {
                if (child != check) {
                    child.Sensitive = check.Active;
                }
            }
        }
    }
}
