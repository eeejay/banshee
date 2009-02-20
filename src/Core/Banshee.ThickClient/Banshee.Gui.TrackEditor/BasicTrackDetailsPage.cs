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
        public int Order {
            get { return 10; }
        }

        public string Title {
            get { return Catalog.GetString ("Basic Details"); }
        }

        public override void LoadTrack (EditorTrackInfo track)
        {
            base.LoadTrack (track);
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

            PageNavigationEntry title_entry = new PageNavigationEntry (Dialog);
            AddField (left, title_entry, null,
                delegate { return Catalog.GetString ("Track _Title:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((PageNavigationEntry)widget).Text = track.TrackTitle; },
                delegate (EditorTrackInfo track, Widget widget) { track.TrackTitle = ((PageNavigationEntry)widget).Text; },
                FieldOptions.NoSync
            );

            PageNavigationEntry track_artist_entry = new PageNavigationEntry (Dialog, "CoreArtists", "Name");
            FieldPage.FieldSlot track_artist_slot = AddField (left, track_artist_entry, 
                Catalog.GetString ("Set all track artists to this value"),
                delegate { return Catalog.GetString ("Track _Artist:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((PageNavigationEntry)widget).Text = track.ArtistName; },
                delegate (EditorTrackInfo track, Widget widget) { track.ArtistName = ((PageNavigationEntry)widget).Text; }
            );

            AlbumArtistEntry album_artist_entry = new AlbumArtistEntry (track_artist_slot.SyncButton, 
                title_entry, track_artist_entry);
            AddField (left, null, album_artist_entry,
                Catalog.GetString ("Set all compilation album artists to these values"), null,
                delegate (EditorTrackInfo track, Widget widget) {
                    AlbumArtistEntry entry = widget as AlbumArtistEntry;
                    entry.IsCompilation = track.IsCompilation;
                    entry.Text = track.AlbumArtist;
                },
                delegate (EditorTrackInfo track, Widget widget) {
                    AlbumArtistEntry entry = widget as AlbumArtistEntry;
                    track.IsCompilation = entry.IsCompilation;
                    track.AlbumArtist = entry.Text;
                }
            );

            AddField (left, new TextEntry ("CoreAlbums", "Title"), 
                Catalog.GetString ("Set all album titles to this value"),
                delegate { return Catalog.GetString ("Albu_m Title:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((TextEntry)widget).Text = track.AlbumTitle; },
                delegate (EditorTrackInfo track, Widget widget) { track.AlbumTitle = ((TextEntry)widget).Text; }
            );

            AddField (left, new GenreEntry (), 
                Catalog.GetString ("Set all genres to this value"),
                delegate { return Catalog.GetString ("_Genre:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((GenreEntry)widget).Value = track.Genre; },
                delegate (EditorTrackInfo track, Widget widget) { track.Genre = ((GenreEntry)widget).Value; }
            );

            // Right

            /* Translators: "of" is the word beteen a track/disc number and the total count. */
            AddField (right, new RangeEntry (Catalog.GetString ("of"), !MultipleTracks 
                ? null as RangeEntry.RangeOrderClosure
                : delegate (RangeEntry entry) {
                    for (int i = 0, n = Dialog.TrackCount; i < n; i++) {
                        EditorTrackInfo track = Dialog.LoadTrack (i);

                        if (Dialog.CurrentTrackIndex == i) {
                            // In this case the writeClosure is invoked, 
                            // which will take care of updating the TrackInfo
                            entry.From.Value = i + 1;
                            entry.To.Value = n;
                        } else {
                            track.TrackNumber = i + 1;
                            track.TrackCount = n;
                        }
                    }
                }, Catalog.GetString ("Automatically set track number and count")), 
                null,
                delegate { return Catalog.GetString ("Track _Number:"); },
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
                // Catalog.GetString ("Automatically set disc number and count"),
                Catalog.GetString ("Set all disc numbers and counts to these values"),
                delegate { return Catalog.GetString ("_Disc Number:"); },
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
                FieldOptions.None
            );

            Label year_label = EditorUtilities.CreateLabel (null);
            album_artist_entry.LabelWidget.SizeAllocated += delegate { 
                year_label.HeightRequest = album_artist_entry.LabelWidget.Allocation.Height; 
            };
            SpinButtonEntry year_entry = new SpinButtonEntry (0, 3000, 1);
            year_entry.Numeric = true;
            AddField (right, year_label, year_entry,
                Catalog.GetString ("Set all years to this value"),
                delegate { return Catalog.GetString ("_Year:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((SpinButtonEntry)widget).Value = track.Year; },
                delegate (EditorTrackInfo track, Widget widget) { track.Year = ((SpinButtonEntry)widget).ValueAsInt; },
                FieldOptions.Shrink
            );

            AddField (right, new RatingEntry (), 
                Catalog.GetString ("Set all ratings to this value"),
                delegate { return Catalog.GetString ("_Rating:"); },
                delegate (EditorTrackInfo track, Widget widget) { ((RatingEntry)widget).Value = track.Rating; },
                delegate (EditorTrackInfo track, Widget widget) { track.Rating = ((RatingEntry)widget).Value; },
                FieldOptions.Shrink | FieldOptions.NoSync
            );
        }
    }
}
