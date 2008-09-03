//
// SongDialog.cs
//
// Authors:
//   Brad Taylor <brad@getcoded.net>
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
    public class SongDialog : BaseDialog
    {
        public SongDialog (PlaylistSource queue) : base (queue, Catalog.GetString ("Play Song"), "song")
        {
        }

        protected override Widget GetItemWidget ()
        {
            TerseTrackListView track_list = new TerseTrackListView ();
            track_list.SetModel (Music.DatabaseTrackModel);
            return track_list;
        }

        protected override void Queue ()
        {
            QueueSource.AddSelectedTracks (Music);
        }

        protected override TrackInfo FirstTrack {
            get { return Music.TrackModel[Music.TrackModel.Selection.FirstIndex]; }
        }

        public override void Destroy ()
        {
            Music.TrackModel.Selection.Clear ();
            base.Destroy ();
        }
    }
}
