//
// SmartPlaylistSource.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006-2007 Gabriel Burt
// Copyright (C) 2007 Novell, Inc.
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
 
using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Playlist;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.SmartPlaylist
{
    public class SmartPlaylistSource : AbstractPlaylistSource
    {
        private static string generic_name = Catalog.GetString ("Smart Playlist");

        private string condition;
        private string order_by;
        private string limit_number;
        private string limit_criterion;

        static SmartPlaylistSource () {
            SourceTable = "CoreSmartPlaylists";
            SourcePrimaryKey = "SmartPlaylistID";
            TrackJoinTable = "CoreSmartPlaylistEntries";
            IconName = "source-smart-playlist";
        }

#region Properties

        public string Condition {
            get { return condition; }
            set { condition = value; }
        }

        public string OrderBy {
            get { return order_by; }
            set { order_by = value; }
        }

        public string LimitNumber {
            get { return limit_number; }
            set { limit_number = value; }
        }

        public string LimitCriterion {
            get { return limit_criterion; }
            set { limit_criterion = value; }
        }

        private string OrderAndLimit {
            get {
                if (OrderBy == null || OrderBy == "")
                    return null;

                if (LimitCriterion == "0")
                    return String.Format ("ORDER BY {0} LIMIT {1}", OrderBy, LimitNumber);
                else
                    return String.Format ("ORDER BY {0}", OrderBy);
            }
        }

#endregion

#region Constructors

        public SmartPlaylistSource (string name) : this (null, name, "", "", "", "")
        {
        }

        // For existing smart playlists that we're loading from the database
        public SmartPlaylistSource (int? dbid, string name, string condition, string order_by, string limit_number, string limit_criterion) :
            base (generic_name, name, dbid, -1, 0)
        {
            Condition = condition;
            OrderBy = order_by;
            LimitNumber = limit_number;
            LimitCriterion = limit_criterion;

            Condition = "(CoreTracks.Rating > 0)";

            //Globals.Library.TrackRemoved += OnLibraryTrackRemoved;

            //if (Globals.Library.IsLoaded)
                //OnLibraryReloaded(Globals.Library, new EventArgs());
            //else
                //Globals.Library.Reloaded += OnLibraryReloaded;

            //ListenToPlaylists();
        }

#endregion

#region AbstractPlaylist overrides

        protected override void Create ()
        {
            DbId = ServiceManager.DbConnection.Execute (new BansheeDbCommand (@"
                INSERT INTO CoreSmartPlaylists
                    (Name, Condition, OrderBy, LimitNumber, LimitCriterion)
                    VALUES (?, ?, ?, ?, ?)",
                Name, Condition, OrderBy, LimitNumber, LimitCriterion
            ));
        }

        protected override void Update ()
        {
            ServiceManager.DbConnection.Execute (new BansheeDbCommand (@"
                UPDATE CoreSmartPlaylists
                    SET Name = ?,
                        Condition = ?,
                        OrderBy = ?,
                        LimitNumber = ?,
                        LimitCriterion = ?
                    WHERE SmartPlaylistID = ?",
                Name, Condition, OrderBy, LimitNumber, LimitCriterion, DbId
            ));
        }

#endregion

#region DatabaseSource overrides

        public override void Reload ()
        {
            // TODO don't actually do this here, do it on a timeout

            // Wipe the member list clean
            ServiceManager.DbConnection.Execute (String.Format (
                "DELETE FROM CoreSmartPlaylistEntries WHERE SmartPlaylistID = {0}", DbId
            ));

            // Repopulate it 
            Console.WriteLine (String.Format (
                @"INSERT INTO CoreSmartPlaylistEntries 
                    SELECT {0} as SmartPlaylistID, TrackId FROM CoreTracks {1} {2}",
                    DbId, PrependCondition("WHERE"), OrderAndLimit
            ));
            ServiceManager.DbConnection.Execute (String.Format (
                @"INSERT INTO CoreSmartPlaylistEntries 
                    SELECT {0} as SmartPlaylistID, TrackId FROM CoreTracks {1} {2}",
                    DbId, PrependCondition("WHERE"), OrderAndLimit
            ));

            base.Reload ();
        }

#endregion

        private string PrependCondition (string with)
        {
            return (Condition == null || Condition == String.Empty) ? " " : with + " (" + Condition + ")";
        }

        public static List<SmartPlaylistSource> LoadAll ()
        {
            List<SmartPlaylistSource> sources = new List<SmartPlaylistSource> ();

            IDataReader reader = ServiceManager.DbConnection.ExecuteReader (
                "SELECT SmartPlaylistID, Name, Condition, OrderBy, LimitNumber, LimitCriterion FROM CoreSmartPlaylists"
            );
            
            while (reader.Read ()) {
                SmartPlaylistSource playlist = new SmartPlaylistSource (
                    Convert.ToInt32 (reader[0]), reader[1] as string,
                    reader[2] as string, reader[3] as string,
                    reader[4] as string, reader[5] as string
                );
                sources.Add (playlist);
            }
            
            reader.Dispose();
            return sources;
        }

    }
}
