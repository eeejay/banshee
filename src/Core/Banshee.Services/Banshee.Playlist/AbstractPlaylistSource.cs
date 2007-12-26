//
// AbstractPlaylistSource.cs
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
using System.Data;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data;
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

        private static string source_table;
        private static string track_join_table;
        private static string icon_name;

        protected static string SourceTable {
            get { return source_table; }
            set { source_table = value; }
        }

        protected static string TrackJoinTable {
            get { return track_join_table; }
            set { track_join_table = value; }
        }

        protected static string TrackJoin {
            get { return String.Format (", {0}", TrackJoinTable); }
        }

        protected static string TrackCondition {
            get {
                return String.Format (
                    " {0}.TrackID = CoreTracks.TrackID AND {0}.PlaylistID = {1}",
                    TrackJoinTable, "{0}"
                );
            }
        }

        protected static string IconName {
            get { return icon_name; }
            set { icon_name = value; }
        }

        protected int? DbId {
            get { return dbid; }
            set {
                if (value == null) {
                    Console.WriteLine ("intializing abstract playlist, but dbid = null!");
                    return;
                }
                Console.WriteLine ("intializing abstract playlist");
                dbid = value;
                track_model.JoinFragment = TrackJoin;
                track_model.Condition = String.Format (TrackCondition, dbid);
                AfterInitialized ();
            }
        }

        public AbstractPlaylistSource (string generic_name, string name) : this (generic_name, name, null, -1, 0)
        {
        }

        public AbstractPlaylistSource (string generic_name, string name, int? dbid, int sortColumn, int sortType) : base (generic_name, name, 500)
        {
            Properties.SetString ("IconName", IconName);
            DbId = dbid;
        }

        public override void Rename (string newName)
        {
            base.Rename (newName);
            Save ();
        }

        public virtual void Save ()
        {
            if (dbid == null || dbid < 0)
                Create ();
            else
                Update ();
        }

        protected abstract void Create ();
        protected abstract void Update ();
    }
}
