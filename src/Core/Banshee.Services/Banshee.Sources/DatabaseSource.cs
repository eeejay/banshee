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
using System.Collections.Generic;

using Mono.Unix;
using Hyena.Data;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Sources
{
    public abstract class DatabaseSource : Source, ITrackModelSource
    {
        protected delegate void TrackRangeHandler (TrackListDatabaseModel model, RangeCollection.Range range);

        protected TrackListDatabaseModel track_model;
        protected AlbumListDatabaseModel album_model;
        protected ArtistListDatabaseModel artist_model;

        protected RateLimiter reload_limiter;
        
        public DatabaseSource (string generic_name, string name, string id, int order) : base (generic_name, name, order)
        {
            string uuid = String.Format ("{0}-{1}", this.GetType().Name, id);
            track_model = new TrackListDatabaseModel (ServiceManager.DbConnection, uuid);
            album_model = new AlbumListDatabaseModel (track_model, ServiceManager.DbConnection, uuid);
            artist_model = new ArtistListDatabaseModel (track_model, ServiceManager.DbConnection, uuid);
            reload_limiter = new RateLimiter (50.0, RateLimitedReload);
        }

#region Public Properties

        public override int Count {
            get { return track_model is IFilterable ? ((IFilterable)track_model).UnfilteredCount : track_model.Count; }
        }

        public override string FilterQuery {
            set {
                base.FilterQuery = value;
                track_model.Filter = value;
                track_model.Refilter ();
                RateLimitedReload ();
            }
        }

        public virtual bool CanRemoveTracks {
            get { return true; }
        }

        public virtual bool CanDeleteTracks {
            get { return true; }
        }

        public override string TrackModelPath {
            get { return DBusServiceManager.MakeObjectPath (track_model); }
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
        
        public virtual bool ShowBrowser { 
            get { return true; }
        }

#endregion

#region Public Methods

        public void Reload ()
        {
            reload_limiter.Execute ();
        }

        protected virtual void RateLimitedReload ()
        {
            track_model.Reload ();
            artist_model.Reload ();
            album_model.Reload ();
            OnUpdated ();
        }

        public virtual void RemoveTrack (int index)
        {
            RemoveTrack (track_model [index] as LibraryTrackInfo);
        }

        public virtual void RemoveTrack (LibraryTrackInfo track)
        {
            throw new NotImplementedException(); 
        }

        // Methods for removing tracks from this source
        /*public virtual void RemoveTracks (IEnumerable<TrackInfo> tracks)
        {
            throw new NotImplementedException(); 
        }*/

        public virtual void RemoveSelectedTracks ()
        {
            RemoveSelectedTracks (track_model);
        }

        public virtual void RemoveSelectedTracks (TrackListDatabaseModel model)
        {
            WithTrackSelection (model, RemoveTrackRange);
        }

        // Methods for deleting tracks from this source
        /*public virtual void DeleteTracks (IEnumerable<TrackInfo> tracks)
        {
            throw new NotImplementedException(); 
        }*/

        public virtual void DeleteSelectedTracks ()
        {
            DeleteSelectedTracks (track_model);
        }

        public virtual void DeleteSelectedTracks (TrackListDatabaseModel model)
        {
            WithTrackSelection (model, DeleteTrackRange);
        }

#endregion
        
#region Protected Methods

        protected void AfterInitialized ()
        {
            Reload ();
            //track_model.Reloaded += OnTrackModelReloaded;
            OnSetupComplete ();
        }

        protected virtual void RemoveTrackRange (TrackListDatabaseModel model, RangeCollection.Range range)
        {
            throw new NotImplementedException(); 
        }


        protected virtual void DeleteTrackRange (TrackListDatabaseModel model, RangeCollection.Range range)
        {
            throw new NotImplementedException(); 
        }

        protected void WithTrackSelection (TrackListDatabaseModel model, TrackRangeHandler handler)
        {
            Selection selection = model.Selection;
            if (selection.Count == 0)
                return;

            lock (track_model) {
                foreach (RangeCollection.Range range in selection.Ranges) {
                    handler (model, range);
                }
                Reload ();
            }
        }

#endregion

#region Private Methods

        private void OnTrackModelReloaded (object o, EventArgs args)
        {
            OnUpdated ();
        }

#endregion

    }
}
