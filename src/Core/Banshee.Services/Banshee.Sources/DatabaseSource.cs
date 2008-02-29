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
using Hyena.Data.Sqlite;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Sources
{
    public abstract class DatabaseSource : Source, ITrackModelSource, IDurationAggregator, IFileSizeAggregator
    {
        protected delegate void TrackRangeHandler (TrackListDatabaseModel model, RangeCollection.Range range);

        protected TrackListDatabaseModel track_model;
        protected AlbumListDatabaseModel album_model;
        protected ArtistListDatabaseModel artist_model;

        protected HyenaSqliteCommand rate_track_range_command;

        protected RateLimiter reload_limiter;
        
        public DatabaseSource (string generic_name, string name, string id, int order) : base (generic_name, name, order)
        {
            string uuid = String.Format ("{0}-{1}", this.GetType().Name, id);
            track_model = new TrackListDatabaseModel (ServiceManager.DbConnection, uuid);
            album_model = new AlbumListDatabaseModel (track_model, ServiceManager.DbConnection, uuid);
            artist_model = new ArtistListDatabaseModel (track_model, ServiceManager.DbConnection, uuid);
            rate_track_range_command= new HyenaSqliteCommand (String.Format (@"
                UPDATE CoreTracks SET Rating = ? WHERE TrackID IN (
                    SELECT ItemID FROM CoreCache WHERE ModelID = {0} LIMIT ?, ?)",
                track_model.CacheId
            ));
            reload_limiter = new RateLimiter (50.0, RateLimitedReload);
        }

#region Public Properties

        public override int Count {
            get { return track_model is IFilterable ? ((IFilterable)track_model).UnfilteredCount : track_model.Count; }
        }

        public override int FilteredCount {
            get { return track_model.Count; }
        }

        public TimeSpan Duration {
            get { return track_model.Duration; }
        }

        public TimeSpan FilteredDuration {
            get { return track_model.FilteredDuration; }
        }

        public long FileSize {
            get { return track_model.FileSize; }
        }

        public long FilteredFileSize {
            get { return track_model.FilteredFileSize; }
        }

        public override string FilterQuery {
            set {
                base.FilterQuery = value;
                track_model.Filter = value;
                track_model.Refilter ();
                Reload ();
            }
        }

        public virtual bool CanRemoveTracks {
            get { return true; }
        }

        public virtual bool CanDeleteTracks {
            get { return true; }
        }
        
        public virtual bool ConfirmRemoveTracks {
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

        public void Reload (double min_interval_ms)
        {
            ThreadAssist.SpawnFromMain (delegate {
                reload_limiter.Execute (min_interval_ms);
            });
        }

        public void Reload ()
        {
            ThreadAssist.SpawnFromMain (delegate {
                reload_limiter.Execute (100.0);
            });
        }

        public virtual void RateLimitedReload ()
        {
            track_model.Reload ();
            artist_model.Reload ();
            album_model.Reload ();
            OnUpdated ();
        }

        protected virtual void ReloadChildren ()
        {
            foreach (Source child in Children) {
                if (child is ITrackModelSource) {
                    (child as ITrackModelSource).Reload ();
                }
            }
        }

        public virtual void RemoveTrack (int index)
        {
            RemoveTrack (track_model [index] as DatabaseTrackInfo);
        }

        public virtual void RemoveTrack (DatabaseTrackInfo track)
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

        public virtual void RateSelectedTracks (int rating)
        {
            RateSelectedTracks (track_model, rating);
        }

        public virtual void RateSelectedTracks (TrackListDatabaseModel model, int rating)
        {
            Selection selection = model.Selection;
            if (selection.Count == 0)
                return;

            lock (model) {
                foreach (RangeCollection.Range range in selection.Ranges) {
                    RateTrackRange (model, range, rating);
                }
                Reload ();
                ReloadChildren ();
            }
        }

#endregion
        
#region Protected Methods

        protected void AfterInitialized ()
        {
            Reload ();
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

        protected virtual void RateTrackRange (TrackListDatabaseModel model, RangeCollection.Range range, int rating)
        {
            rate_track_range_command.ApplyValues (rating, range.Start, range.End - range.Start + 1);
            ServiceManager.DbConnection.Execute (rate_track_range_command);
        }

        protected void WithTrackSelection (TrackListDatabaseModel model, TrackRangeHandler handler)
        {
            Selection selection = model.Selection;
            if (selection.Count == 0)
                return;

            lock (model) {
                foreach (RangeCollection.Range range in selection.Ranges) {
                    handler (model, range);
                }
                Reload ();
                ReloadChildren ();
            }
        }

#endregion

    }
}
