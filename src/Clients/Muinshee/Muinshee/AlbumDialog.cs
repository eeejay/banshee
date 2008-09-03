//
// MuinsheeActions.cs
//
// Authors:
//   Brad Taylor <brad@getcoded.net>
//   Gabriel Burt  <gburt@novell.com>
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
using Hyena;
using Hyena.Data;
using Hyena.Widgets;

using Banshee.Gui;
using Banshee.Widgets;
using Banshee.Gui.Dialogs;
using Banshee.Playlist;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Collection.Database;
using Banshee.PlaybackController;
using Banshee.MediaEngine;

namespace Muinshee
{
    internal class MuinsheeAlbumView : AlbumListView
    {
        public MuinsheeAlbumView ()
        {
            HeaderVisible = false;
            ForceDragSourceSet = false;
        }

        protected override void OnRowActivated (object o, EventArgs args)
        {
        }
    }

    public class AlbumDialog : BaseDialog
    {
        private static DatabaseAlbumListModel album_model;

        static AlbumDialog () {
            // TODO set the Album filter as the one/only current filter
            foreach (IFilterListModel filter in Music.CurrentFilters) {
                if (filter is DatabaseAlbumListModel) {
                    album_model = filter as DatabaseAlbumListModel;
                }
            }
        }

        public AlbumDialog (PlaylistSource queue) : base (queue, Catalog.GetString ("Play Album"), "album")
        {
        }

        protected override Widget GetItemWidget ()
        {
            AlbumListView album_view = new AlbumListView ();
            album_view.SetModel (album_model);
            return album_view;
        }

        protected override void Queue ()
        {
            QueueSource.AddAllTracks (Music);
        }

        protected override TrackInfo FirstTrack {
            get { return Music.TrackModel[0]; }
        }

        public override void Destroy ()
        {
            album_model.Selection.Clear ();
            base.Destroy ();
        }

    }
}
