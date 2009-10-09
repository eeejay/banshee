//
// ItemSource.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;

using Mono.Unix;

using Hyena.Collections;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.Database;
using Banshee.Gui;
using Banshee.Library;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Playlist;
using Banshee.Preferences;
using Banshee.ServiceStack;
using Banshee.Sources;

using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class ItemSource : Banshee.Sources.Source, ITrackModelSource, IDurationAggregator, IFileSizeAggregator
    {
        private Item item;
        private MemoryTrackListModel track_model;

        public ItemSource (string name, string id) : base (name, name, 40, "internet-archive-" + id)
        {
            item = Item.LoadOrCreate (id, name);
            track_model = new MemoryTrackListModel ();
            track_model.Reloaded += delegate { OnUpdated (); };

            Properties.Set<Gtk.Widget> ("Nereid.SourceContents", new ItemSourceContents (this, item));
        }

        public void Reload ()
        {
        }

        public override int Count {
            get { return 0; }
        }

        public override int FilteredCount {
            get { return track_model.Count; }
        }

        public TimeSpan Duration {
            get {
                TimeSpan duration = TimeSpan.Zero;
                foreach (var t in track_model) {
                    duration += t.Duration;
                }
                return duration;
            }
        }

        public long FileSize {
            get { return track_model.Sum (t => t.FileSize); }
        }

#region ITrackModelSource

        public TrackListModel TrackModel {
            get { return track_model; }
        }

        public bool HasDependencies { get { return false; } }

        public void RemoveSelectedTracks () {}
        public void DeleteSelectedTracks () {}

        public bool ConfirmRemoveTracks { get { return false; } }

        public bool Indexable { get { return false; } }

        public bool ShowBrowser { 
            get { return false; }
        }

        public bool CanShuffle {
            get { return false; }
        }

        public bool CanRepeat {
            get { return false; }
        }

        public bool CanAddTracks {
            get { return false; }
        }

        public bool CanRemoveTracks {
            get { return false; }
        }

        public bool CanDeleteTracks {
            get { return false; }
        }

#endregion

        public override bool HasEditableTrackProperties {
            get { return false; }
        }

        public override bool HasViewableTrackProperties {
            get { return false; }
        }

        public override bool CanSearch {
            get { return false; }
        }
    }
}
