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
using Hyena.Data;
using Hyena.Query;
using Hyena.Data.Sqlite;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
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

    public abstract class PrimarySource : DatabaseSource
    {
        protected ErrorSource error_source = new ErrorSource (Catalog.GetString ("Import Errors"));
        protected bool error_source_visible = false;

        protected string remove_range_sql = @"
            INSERT INTO CoreRemovedTracks SELECT ?, TrackID, Uri FROM CoreTracks WHERE TrackID IN ({0});
            DELETE FROM CoreTracks WHERE TrackID IN ({0})";

        protected int source_id;
        public int SourceId {
            get { return source_id; }
        }

        public ErrorSource ErrorSource {
            get { return error_source; }
        }

        public delegate void TrackEventHandler (Source sender, TrackEventArgs args);

        public event TrackEventHandler TracksAdded;
        public event TrackEventHandler TracksChanged;
        public event TrackEventHandler TracksRemoved;

        private static Dictionary<int, PrimarySource> primary_sources = new Dictionary<int, PrimarySource> ();
        public static PrimarySource GetById (int id)
        {
            return (primary_sources.ContainsKey (id)) ? primary_sources[id] : null;
        }

        protected PrimarySource (string generic_name, string name, string id, int order) : base (generic_name, name, id, order)
        {
            source_id = ServiceManager.DbConnection.Query<int> ("SELECT SourceID FROM CorePrimarySources WHERE StringID = ?", id);
            if (source_id == 0) {
                source_id = ServiceManager.DbConnection.Execute ("INSERT INTO CorePrimarySources (StringID) VALUES (?)", id);
            }

            track_model.Condition = String.Format ("CoreTracks.SourceID = {0}", source_id);
            error_source.Updated += OnErrorSourceUpdated;
            OnErrorSourceUpdated (null, null);

            primary_sources[source_id] = this;
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

        internal void NotifyTracksRemoved ()
        {
            OnTracksRemoved ();
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

        /*public override void RemoveTracks (IEnumerable<TrackInfo> tracks)
        {

            // BEGIN transaction

            int i = 0;
            DatabaseTrackInfo ltrack;
            foreach (TrackInfo track in tracks) {
                ltrack = track as DatabaseTrackInfo;
                if (ltrack == null)
                    continue;

                command.ApplyValues (ltrack.DbId, ltrack.DbId, ltrack.DbId);
                ServiceManager.DbConnection.Execute (command);

                if (++i % 100 == 0) {
                    // COMMIT and BEGIN new transaction
                }
            }

            // COMMIT transaction

            // Reload the library, all playlists, etc
            Reload ();
        }*/

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

        protected override void OnTracksRemoved ()
        {
            ThreadAssist.SpawnFromMain (delegate {
                Reload ();

                TrackEventHandler handler = TracksRemoved;
                if (handler != null) {
                    handler (this, new TrackEventArgs ());
                }
            });
        }

        protected override void RemoveTrackRange (TrackListDatabaseModel model, RangeCollection.Range range)
        {
            ServiceManager.DbConnection.Execute (
                String.Format (remove_range_sql, model.TrackIdsSql),
                DateTime.Now,
                model.CacheId, range.Start, range.End - range.Start + 1,
                model.CacheId, range.Start, range.End - range.Start + 1
            );
        }
    }
}
