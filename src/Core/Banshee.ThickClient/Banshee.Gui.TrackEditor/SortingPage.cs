//
// SortingPage.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
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
    public class SortingPage : FieldPage, ITrackEditorPage
    {        
        public int Order {
            get { return 30; }
        }
                                    
        public string Title {
            get { return Catalog.GetString ("Sorting"); }
        }
        
        protected override void AddFields ()
        {
            AddField (this, new TextEntry (),
                Catalog.GetString ("Set all sort track titles to this value"),
                delegate { return Catalog.GetString ("Sort Track Title:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.TrackTitleSort; },
                delegate (EditorTrackInfo track, Widget widget) {  track.TrackTitleSort = ((TextEntry)widget).Text; }
            );
            
            AddField (this, new TextEntry (),
                Catalog.GetString ("Set all sort track artists to this value"),
                delegate { return Catalog.GetString ("Sort Track Artist:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.ArtistNameSort; },
                delegate (EditorTrackInfo track, Widget widget) {  track.ArtistNameSort = ((TextEntry)widget).Text; }
            );
            
            AddField (this, new TextEntry (),
                Catalog.GetString ("Set all sort album artists to this value"),
                delegate { return Catalog.GetString ("Sort Album Artist:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.AlbumArtistSort; },
                delegate (EditorTrackInfo track, Widget widget) {  track.AlbumArtistSort = ((TextEntry)widget).Text; }
            );
            
            AddField (this, new TextEntry (),
                Catalog.GetString ("Set all sort album titles to this value"),
                delegate { return Catalog.GetString ("Sort Album Title:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.AlbumTitleSort; },
                delegate (EditorTrackInfo track, Widget widget) {  track.AlbumTitleSort = ((TextEntry)widget).Text; }
            );
        }
    }
}
