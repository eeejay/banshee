//
// AbstractPlaylistSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Data;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Database;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Playlist
{
    public abstract class AbstractPlaylistSource : DatabaseSource
    {
        protected int? dbid;

        protected abstract string SourceTable { get; }
        protected abstract string SourcePrimaryKey { get; }
        protected abstract string TrackJoinTable { get; }

        protected DateTime last_added = DateTime.MinValue;
        protected DateTime last_updated = DateTime.MinValue;
        protected DateTime last_removed = DateTime.MinValue;

        protected virtual string TrackCondition {
            get {
                return String.Format (
                    " {0}.TrackID = CoreTracks.TrackID AND {0}.{2} = {1}",
                    TrackJoinTable, "{0}", SourcePrimaryKey
                );
            }
        }

        protected virtual string JoinPrimaryKey {
            get { return "EntryID"; }
        }

        protected virtual bool CachesJoinTableEntries {
            get { return true; }
        }

        public int? DbId {
            get { return dbid; }
            protected set {
                if (value == null) {
                    return;
                }
                dbid = value;
                track_model.JoinTable = TrackJoinTable;
                track_model.JoinPrimaryKey = JoinPrimaryKey;
                track_model.JoinColumn = "TrackID";
                track_model.CachesJoinTableEntries = CachesJoinTableEntries;
                track_model.Condition = String.Format (TrackCondition, dbid);
                AfterInitialized ();

                count_updated_command = new HyenaSqliteCommand (String.Format (
                    @"SELECT COUNT(*) FROM {0} WHERE {1} = {2} AND TrackID IN (
                        SELECT TrackID FROM CoreTracks WHERE DateUpdatedStamp > ?)",
                    TrackJoinTable, SourcePrimaryKey, dbid
                ));

                count_removed_command = new HyenaSqliteCommand (String.Format (
                    @"SELECT COUNT(*) FROM {0} WHERE {1} = {2} AND TrackID IN (
                        SELECT TrackID FROM CoreRemovedTracks WHERE DateRemovedStamp > ?)",
                    TrackJoinTable, SourcePrimaryKey, dbid
                ));
            }
        }

        protected int primary_source_id;
        public int PrimarySourceId {
            get { return primary_source_id; }
        }

        public PrimarySource PrimarySource {
            get { return PrimarySource.GetById (primary_source_id); }
            set { primary_source_id = value.DbId; }
        }

        protected HyenaSqliteCommand count_updated_command;
        protected HyenaSqliteCommand count_removed_command;

        public AbstractPlaylistSource (string generic_name, string name, int primarySourceId)
            : this (generic_name, name, null, -1, 0, primarySourceId)
        {
        }

        public AbstractPlaylistSource (string generic_name, string name, int? dbid, int sortColumn, int sortType, int primarySourceId)
            : base (generic_name, name, Convert.ToString (dbid), 500)
        {
            this.primary_source_id = primarySourceId;
        }

        public override void Rename (string newName)
        {
            base.Rename (newName);
            Save ();
        }

        public virtual void Save ()
        {
            if (dbid == null || dbid <= 0)
                Create ();
            else
                Update ();
        }

        public void NotifyUpdated ()
        {
            OnUserNotifyUpdated ();
        }

        protected abstract void Create ();
        protected abstract void Update ();
    }
}
