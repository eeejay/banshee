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

        private QueryField changed_field;
        public QueryField ChangedField {
            get { return changed_field; }
        }

        public TrackEventArgs ()
        {
            when = DateTime.Now;
        }

        public TrackEventArgs (QueryField field) : this ()
        {
            this.changed_field = field;
        }
    }

    public abstract class PrimarySource : DatabaseSource, IDisposable
    {
        protected ErrorSource error_source;
        protected bool error_source_visible = false;

        protected string remove_range_sql = @"
            INSERT INTO CoreRemovedTracks SELECT ?, TrackID, Uri FROM CoreTracks WHERE TrackID IN ({0});
            DELETE FROM CoreTracks WHERE TrackID IN ({0})";

        protected HyenaSqliteCommand prune_artists_albums_command = new HyenaSqliteCommand (@"
            DELETE FROM CoreArtists WHERE ArtistID NOT IN (SELECT ArtistID FROM CoreTracks);
            DELETE FROM CoreAlbums WHERE AlbumID NOT IN (SELECT AlbumID FROM CoreTracks)
        ");

        protected int dbid;
        public int DbId {
            get { return dbid; }
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
            type_unique_id = id;
            PrimarySourceInitialize ();
        }

        protected PrimarySource () : base ()
        {
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
            dbid = ServiceManager.DbConnection.Query<int> ("SELECT PrimarySourceID FROM CorePrimarySources WHERE StringID = ?", TypeUniqueId);
            if (dbid == 0) {
                dbid = ServiceManager.DbConnection.Execute ("INSERT INTO CorePrimarySources (StringID) VALUES (?)", TypeUniqueId);
            }

            track_model.Condition = String.Format ("CoreTracks.PrimarySourceID = {0}", dbid);

            primary_sources[dbid] = this;
            
            foreach (PlaylistSource pl in PlaylistSource.LoadAll ())
                if (pl.PrimarySourceId == dbid)
                    AddChildSource (pl);

            foreach (SmartPlaylistSource pl in SmartPlaylistSource.LoadAll ())
                if (pl.PrimarySourceId == dbid)
                    AddChildSource (pl);
        }

        internal void NotifyTracksAdded ()
        {
            OnTracksAdded ();
        }

        internal void NotifyTracksChanged (QueryField field)
        {
            OnTracksChanged (field);
        }

        internal void NotifyTracksChanged ()
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

        protected override void OnTracksChanged (QueryField field)
        {
            ThreadAssist.SpawnFromMain (delegate {
                Reload ();

                TrackEventHandler handler = TracksChanged;
                if (handler != null) {
                    handler (this, new TrackEventArgs (field));
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

        protected override void RemoveTrackRange (DatabaseTrackListModel model, RangeCollection.Range range)
        {
            ServiceManager.DbConnection.Execute (
                String.Format (remove_range_sql, model.TrackIdsSql),
                DateTime.Now,
                model.CacheId, range.Start, range.End - range.Start + 1,
                model.CacheId, range.Start, range.End - range.Start + 1
            );
        }

        protected override void DeleteTrackRange (DatabaseTrackListModel model, RangeCollection.Range range)
        {
            // Remove from file system
            for (int i = range.Start; i <= range.End; i++) {
                DatabaseTrackInfo track = model [i] as DatabaseTrackInfo;
                if (track == null)
                    continue;

                try {
                    DeleteTrack (track);
                } catch (Exception e) {
                    Log.Exception (e);
                    ErrorSource.AddMessage (e.Message, track.Uri.ToString ());
                }
            }

            // Remove from database
            RemoveTrackRange (model, range);
        }

        protected virtual void DeleteTrack (DatabaseTrackInfo track)
        {
            throw new Exception ("PrimarySource DeleteTrack method not implemented");
        }

        public override bool AcceptsInputFromSource (Source source)
        {
            return base.AcceptsInputFromSource (source) && source.Parent != this;
        }

        public override void MergeSourceInput (Source source, SourceMergeType mergeType)
        {
            AddSelectedTracks (source);
            /*if (!(source is IImportSource) || mergeType != SourceMergeType.Source) {
                return;
            }
            
            ((IImportSource)source).Import ();
            */
        }

        protected override void AddTrackRange (DatabaseTrackListModel model, RangeCollection.Range range)
        {
            for (int i = range.Start; i <= range.End; i++) {
                DatabaseTrackInfo track = model [i] as DatabaseTrackInfo;
                if (track == null)
                    continue;

                try {
                    AddTrack (track);
                } catch (Exception e) {
                    Log.Exception (e);
                    ErrorSource.AddMessage (e.Message, track.Uri.ToString ());
                }
            }
        }

        protected virtual void AddTrack (DatabaseTrackInfo track)
        {
            throw new Exception ("PrimarySource DeleteTrack method not implemented");
        }

        protected override void PruneArtistsAlbums ()
        {
            ServiceManager.DbConnection.Execute (prune_artists_albums_command);
            base.PruneArtistsAlbums ();
        }
    }
}
