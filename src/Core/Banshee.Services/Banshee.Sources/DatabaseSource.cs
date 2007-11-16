//
// DatabaseSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Sources
{
    public class DatabaseSource : Source, ITrackModelSource
    {
        protected TrackListDatabaseModel track_model;
        protected AlbumListDatabaseModel album_model;
        protected ArtistListDatabaseModel artist_model;
        
        public DatabaseSource (string name, int order) : base (name, order)
        {
            track_model = new TrackListDatabaseModel (ServiceManager.DbConnection);
            album_model = new AlbumListDatabaseModel (track_model, ServiceManager.DbConnection);
            artist_model = new ArtistListDatabaseModel (track_model, ServiceManager.DbConnection);
        }
        
        protected void AfterInitialized ()
        {
            track_model.Reload ();
            artist_model.Reload ();
            album_model.Reload ();
            
            track_model.Reloaded += OnTrackModelReloaded;
            
            OnSetupComplete ();
        }

        private void OnTrackModelReloaded (object o, EventArgs args)
        {
            OnUpdated ();
        }

        public override int Count {
            get { return track_model.Count; }
        }
        
        public TrackListModel TrackModel {
            get { return track_model; }
        }
        
        public AlbumListModel AlbumModel {
            get { return album_model; }
        }
        
        public ArtistListModel ArtistModel {
            get { return artist_model; }
        }
        
        public override string TrackModelPath {
            get { return DBusServiceManager.MakeObjectPath (track_model); }
        }
    }
}
