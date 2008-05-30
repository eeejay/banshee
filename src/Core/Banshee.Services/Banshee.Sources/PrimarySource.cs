//
// PrimarySource.cs
//
// Author:
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
using System.Collections.Generic;
using Mono.Unix;

using Hyena;
using Hyena.Data;
using Hyena.Query;
using Hyena.Data.Sqlite;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Sources;
using Banshee.Playlist;
using Banshee.SmartPlaylist;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Query;

namespace Banshee.Sources
{
    public class TrackEventArgs : EventArgs
    {
        private DateTime when;
        public DateTime When {
            get { return when; }
        }

        private QueryField [] changed_fields;
        public QueryField [] ChangedFields {
            get { return changed_fields; }
        }

        public TrackEventArgs ()
        {
            when = DateTime.Now;
        }

        public TrackEventArgs (params QueryField [] fields) : this ()
        {
            changed_fields = fields;
        }
    }

    public abstract class PrimarySource : DatabaseSource, IDisposable
    {
        protected ErrorSource error_source;
        protected bool error_source_visible = false;

        protected string remove_range_sql = @"
            INSERT INTO CoreRemovedTracks SELECT ?, TrackID, Uri FROM CoreTracks WHERE TrackID IN (SELECT {0});
            DELETE FROM CoreTracks WHERE TrackID IN (SELECT {0})";

        protected HyenaSqliteCommand remove_list_command = new HyenaSqliteCommand (@"
            INSERT INTO CoreRemovedTracks SELECT ?, TrackID, Uri FROM CoreTracks WHERE TrackID IN (SELECT ItemID FROM CoreCache WHERE ModelID = ?);
            DELETE FROM CoreTracks WHERE TrackID IN (SELECT ItemID FROM CoreCache WHERE ModelID = ?)
        ");

        protected HyenaSqliteCommand prune_artists_albums_command = new HyenaSqliteCommand (@"
            DELETE FROM CoreArtists WHERE ArtistID NOT IN (SELECT ArtistID FROM CoreTracks);
            DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT AlbumID FROM CoreTracks)
        ");
        
        protected HyenaSqliteCommand purge_tracks_command = new HyenaSqliteCommand (@"
            DELETE FROM CoreTracks WHERE PrimarySourceId = ?
        ");

        public readonly SchemaEntry<bool> ExpandedSchema;

        private int dbid;
        public int DbId {
            get {
                if (dbid > 0) {
                    return dbid;
                }
                
                dbid = ServiceManager.DbConnection.Query<int> ("SELECT PrimarySourceID FROM CorePrimarySources WHERE StringID = ?", UniqueId);
                if (dbid == 0) {
                    dbid = ServiceManager.DbConnection.Execute ("INSERT INTO CorePrimarySources (StringID) VALUES (?)", UniqueId);
                } else {
                    SavedCount = ServiceManager.DbConnection.Query<int> ("SELECT CachedCount FROM CorePrimarySources WHERE PrimarySourceID = ?", dbid);
                }
                
                if (dbid == 0) {
                    throw new ApplicationException ("dbid could not be resolved, this should never happen");
                }
                
                return dbid;
            }
        }

        public ErrorSource ErrorSource {
            get {
                if (error_source == null) {
                    error_source = new ErrorSource (Catalog.GetString ("Errors"));
                    ErrorSource.Updated += OnErrorSourceUpdated;
                    OnErrorSourceUpdated (null, null);
                }
                return error_source;
            }
        }


        private bool is_local = false;
        public bool IsLocal {
            get { return is_local; }
            protected set { is_local = value; }
        }

        public delegate void TrackEventHandler (Source sender, TrackEventArgs args);

        public event TrackEventHandler TracksAdded;
        public event TrackEventHandler TracksChanged;
        public event TrackEventHandler TracksDeleted;

        private static Dictionary<int, PrimarySource> primary_sources = new Dictionary<int, PrimarySource> ();
        public static PrimarySource GetById (int id)
        {
            return (primary_sources.ContainsKey (id)) ? primary_sources[id] : null;
        }

        public virtual SafeUri UriAndTypeToSafeUri (TrackUriType type, string uri_field)
        {
            if (type == TrackUriType.RelativePath && BaseDirectory != null)
                return new SafeUri (System.IO.Path.Combine (BaseDirectory, uri_field));
            else
                return new SafeUri (uri_field);
        }

        public virtual void UriToFields (SafeUri uri, out TrackUriType type, out string uri_field)
        {
            uri_field = Paths.MakePathRelative (uri.AbsolutePath, BaseDirectory);
            type = (uri_field == null) ? TrackUriType.AbsoluteUri : TrackUriType.RelativePath;
            if (uri_field == null) {
                uri_field = uri.AbsoluteUri;
            }
        }

        public virtual string BaseDirectory {
            get { return null; }
        }

        protected PrimarySource (string generic_name, string name, string id, int order) : base (generic_name, name, id, order)
        {
            PrimarySourceInitialize ();

            ExpandedSchema = new SchemaEntry<bool> (
                String.Format ("sources.{0}", ConfigurationId), "expanded", true, "Is source expanded", "Is source expanded"
            );
        }

        protected PrimarySource () : base ()
        {
            ExpandedSchema = new SchemaEntry<bool> (
                String.Format ("sources.{0}", ConfigurationId), "expanded", true, "Is source expanded", "Is source expanded"
            );
        }

        public override bool? AutoExpand {
            get { return ExpandedSchema.Get (); }
        }

        public override bool Expanded {
            get { return ExpandedSchema.Get (); }
            set { ExpandedSchema.Set (value); }
        }


        public virtual void Dispose ()
        {
            if (Application.ShuttingDown)
                return;

            DatabaseTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (track != null && track.PrimarySourceId == this.DbId) {
                ServiceManager.PlayerEngine.Close ();
            }

            ClearChildSources ();
            ServiceManager.SourceManager.RemoveSource (this);
        }

        protected override void Initialize ()
        {
            base.Initialize ();
            PrimarySourceInitialize ();
        }

        private void PrimarySourceInitialize ()
        {
            // Scope the tracks to this primary source
            track_model.AddCondition (String.Format ("CoreTracks.PrimarySourceID = {0}", DbId));

            primary_sources[DbId] = this;
            
            // Load our playlists and smart playlists
            foreach (PlaylistSource pl in PlaylistSource.LoadAll (DbId)) {
                AddChildSource (pl);
            }

            int sp_count = 0;
            foreach (SmartPlaylistSource pl in SmartPlaylistSource.LoadAll (DbId)) {
                AddChildSource (pl);
                sp_count++;
            }

            // Create default smart playlists if we haven't done it ever before, and if the
            // user has zero smart playlists.
            if (!HaveCreatedSmartPlaylists) {
                if (sp_count == 0) {
                    foreach (SmartPlaylistDefinition def in DefaultSmartPlaylists) {
                        SmartPlaylistSource pl = def.ToSmartPlaylistSource (this);
                        pl.Save ();
                        AddChildSource (pl);
                        pl.RefreshAndReload ();
                        sp_count++;
                    }
                }

                // Only save it if we already had some smart playlists, or we actually created some (eg not
                // if we didn't have any and the list of default ones is empty atm).
                if (sp_count > 0)
                    HaveCreatedSmartPlaylists = true;
            }
        }

        private bool HaveCreatedSmartPlaylists {
            get { return DatabaseConfigurationClient.Client.Get<bool> ("HaveCreatedSmartPlaylists", UniqueId, false); }
            set { DatabaseConfigurationClient.Client.Set<bool> ("HaveCreatedSmartPlaylists", UniqueId, value); }
        }

        public override void Save ()
        {
            ServiceManager.DbConnection.Execute (
                "UPDATE CorePrimarySources SET CachedCount = ? WHERE PrimarySourceID = ?",
                Count, DbId
            );
        }

        public virtual void CopyTrackTo (DatabaseTrackInfo track, SafeUri uri, BatchUserJob job)
        {
            Log.WarningFormat ("CopyToTrack not implemented for source {0}", this);
        }

        internal void NotifyTracksAdded ()
        {
            OnTracksAdded ();
        }

        internal void NotifyTracksChanged (params QueryField [] fields)
        {
            OnTracksChanged (fields);
        }

        // TODO replace this public method with a 'transaction'-like system
        public void NotifyTracksChanged ()
        {
            OnTracksChanged ();
        }

        internal void NotifyTracksDeleted ()
        {
            OnTracksDeleted ();
        }

        protected void OnErrorSourceUpdated (object o, EventArgs args)
        {
            if (error_source.Count > 0 && !error_source_visible) {
                AddChildSource (error_source);
                error_source_visible = true;
            } else if (error_source.Count <= 0 && error_source_visible) {
                RemoveChildSource (error_source);
                error_source_visible = false;
            }
        }

        public virtual IEnumerable<SmartPlaylistDefinition> DefaultSmartPlaylists {
            get { yield break; }
        }

        public virtual IEnumerable<SmartPlaylistDefinition> NonDefaultSmartPlaylists {
            get { yield break; }
        }

        public IEnumerable<SmartPlaylistDefinition> PredefinedSmartPlaylists {
            get {
                foreach (SmartPlaylistDefinition def in DefaultSmartPlaylists)
                    yield return def;

                foreach (SmartPlaylistDefinition def in NonDefaultSmartPlaylists)
                    yield return def;
            }
        }

        public override bool CanSearch {
            get { return true; }
        }

        public override void SetParentSource (Source source)
        {
            if (source is PrimarySource) {
                throw new ArgumentException ("PrimarySource cannot have another PrimarySource as its parent");
            }

            base.SetParentSource (source);
        }

        protected override void OnTracksAdded ()
        {
            ThreadAssist.SpawnFromMain (delegate {
                Reload ();

                TrackEventHandler handler = TracksAdded;
                if (handler != null) {
                    handler (this, new TrackEventArgs ());
                }
            });
        }

        protected override void OnTracksChanged (params QueryField [] fields)
        {
            ThreadAssist.SpawnFromMain (delegate {
                if (NeedsReloadWhenFieldsChanged (fields)) {
                    Reload ();
                } else {
                    InvalidateCaches ();
                }

                System.Threading.Thread.Sleep (150);

                TrackEventHandler handler = TracksChanged;
                if (handler != null) {
                    handler (this, new TrackEventArgs (fields));
                }
            });
        }

        protected override void OnTracksDeleted ()
        {
            ThreadAssist.SpawnFromMain (delegate {
                PruneArtistsAlbums ();
                Reload ();

                TrackEventHandler handler = TracksDeleted;
                if (handler != null) {
                    handler (this, new TrackEventArgs ());
                }
            });
        }

        protected override void OnTracksRemoved ()
        {
            OnTracksDeleted ();
        }
        
        protected virtual void PurgeTracks ()
        {
            ServiceManager.DbConnection.Execute (purge_tracks_command, DbId);
        }

        protected override void RemoveTrackRange (DatabaseTrackListModel model, RangeCollection.Range range)
        {
            ServiceManager.DbConnection.Execute (
                String.Format (remove_range_sql, model.TrackIdsSql),
                DateTime.Now,
                model.CacheId, range.Start, range.End - range.Start + 1,
                model.CacheId, range.Start, range.End - range.Start + 1
            );
        }

        public void DeleteSelectedTracksFromChild (DatabaseSource source)
        {
            if (source.Parent != this)
                return;

            DeleteSelectedTracks (source.TrackModel as DatabaseTrackListModel);
        }

        protected override void DeleteSelectedTracks (DatabaseTrackListModel model)
        {
            ThreadAssist.SpawnFromMain (delegate {
                if (model == null)
                    return;

                CachedList<DatabaseTrackInfo> list = CachedList<DatabaseTrackInfo>.CreateFromModelSelection (model);
                DeleteTrackList (list);

                OnTracksDeleted ();
                OnUserNotifyUpdated ();
                ThreadAssist.ProxyToMain (delegate {
                    OnUpdated ();
                });
            });
        }

        protected virtual void DeleteTrackList (CachedList<DatabaseTrackInfo> list)
        {
            is_deleting = true;
            DeleteTrackJob.Total += (int) list.Count;

            // Remove from file system
            foreach (DatabaseTrackInfo track in list) {
                if (track == null) {
                    DeleteTrackJob.Completed++;
                    continue;
                }

                try {
                    DeleteTrackJob.Status = String.Format ("{0} - {1}", track.ArtistName, track.TrackTitle);
                    DeleteTrack (track);
                } catch (Exception e) {
                    Log.Exception (e);
                    ErrorSource.AddMessage (e.Message, track.Uri.ToString ());
                }

                DeleteTrackJob.Completed++;
                if (DeleteTrackJob.Completed % 10 == 0 && !DeleteTrackJob.IsFinished) {
                    OnTracksDeleted ();
                }
            }

            is_deleting = false;

            if (DeleteTrackJob.Total == DeleteTrackJob.Completed) {
                delete_track_job.Finish ();
                delete_track_job = null;
            }

            // Remove from database
            ServiceManager.DbConnection.Execute (remove_list_command, DateTime.Now, list.CacheId, list.CacheId);
        }

        protected virtual void DeleteTrack (DatabaseTrackInfo track)
        {
            throw new Exception ("PrimarySource DeleteTrack method not implemented");
        }

        public override bool AcceptsInputFromSource (Source source)
        {
            return base.AcceptsInputFromSource (source) && source.Parent != this
                && (source.Parent is PrimarySource || source is PrimarySource)
                && !(source.Parent is Banshee.Library.LibrarySource);
        }

        public override bool AddSelectedTracks (Source source)
        {
            if (!AcceptsInputFromSource (source))
                return false;

            DatabaseTrackListModel model = (source as ITrackModelSource).TrackModel as DatabaseTrackListModel;

            // Store a snapshot of the current selection
            CachedList<DatabaseTrackInfo> cached_list = CachedList<DatabaseTrackInfo>.CreateFromModelSelection (model);

            System.Threading.ThreadPool.QueueUserWorkItem (AddTrackList, cached_list);

            return true;
        }

        private bool is_adding;
        public bool IsAdding {
            get { return is_adding; }
        }

        private bool is_deleting;
        public bool IsDeleting {
            get { return is_deleting; }
        }

        protected virtual void AddTrackAndIncrementCount (DatabaseTrackInfo track)
        {
            AddTrackJob.Status = String.Format ("{0} - {1}", track.ArtistName, track.TrackTitle);
            AddTrack (track);
            IncrementAddedTracks ();
        }

        protected virtual void AddTrackList (object cached_list)
        {
            CachedList<DatabaseTrackInfo> list = cached_list as CachedList<DatabaseTrackInfo>;
            is_adding = true;
            AddTrackJob.Total += (int) list.Count;

            foreach (DatabaseTrackInfo track in list) {
                if (track == null) {
                    IncrementAddedTracks ();
                    continue;
                }

                try {
                    AddTrackJob.Status = String.Format ("{0} - {1}", track.ArtistName, track.TrackTitle);
                    AddTrackAndIncrementCount (track);
                } catch (Exception e) {
                    IncrementAddedTracks ();
                    Log.Exception (e);
                    ErrorSource.AddMessage (e.Message, track.Uri.ToString ());
                }
            }
            is_adding = false;
        }

        protected void IncrementAddedTracks ()
        {
            bool finished = false, notify = false;

            lock (this) {
                add_track_job.Completed++;

                if (add_track_job.IsFinished) {
                    finished = true;
                    add_track_job = null;
                } else {
                    add_track_job.Status = String.Empty;
                    if (add_track_job.Completed % 10 == 0)
                        notify = true;
                }
            }

            if (notify || finished)
                OnTracksAdded ();

            if (finished)
                OnUserNotifyUpdated ();
        }

        private bool delay_add_job = true;
        protected bool DelayAddJob {
            get { return delay_add_job; }
            set { delay_add_job = value; }
        }

        private bool delay_delete_jbo = true;
        protected bool DelayDeleteJob {
            get { return delay_delete_jbo; }
            set { delay_delete_jbo = value; }
        }

        private BatchUserJob add_track_job;
        protected BatchUserJob AddTrackJob {
            get {
                lock (this) {
                    if (add_track_job == null) {
                        add_track_job = new BatchUserJob (String.Format (Catalog.GetString (
                            "Adding {0} of {1} to {2}"), "{0}", "{1}", Name), 
                            Properties.GetStringList ("Icon.Name"));
                        add_track_job.DelayShow = DelayAddJob;
                        add_track_job.Register ();
                    }
                }
                return add_track_job;
            }
        }

        private BatchUserJob delete_track_job;
        protected BatchUserJob DeleteTrackJob {
            get {
                lock (this) {
                    if (delete_track_job == null) {
                        delete_track_job = new BatchUserJob (String.Format (Catalog.GetString (
                            "Deleting {0} of {1} From {2}"), "{0}", "{1}", Name),
                            Properties.GetStringList ("Icon.Name"));
                        delete_track_job.DelayShow = DelayDeleteJob;
                        delete_track_job.Register ();
                    }
                }
                return delete_track_job;
            }
        }

        protected override void PruneArtistsAlbums ()
        {
            ServiceManager.DbConnection.Execute (prune_artists_albums_command);
            base.PruneArtistsAlbums ();
        }
    }
}
