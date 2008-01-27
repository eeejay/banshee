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

using Hyena.Data.Query;
using Hyena.Data.Sqlite;
 
using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Playlist;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.SmartPlaylist
{
    public struct Order
    {
        public string Label, Key, Dir;

        public Order (string key, string label, string dir)
        {
            Key = key;
            Label = label;
            Dir = dir;
        }
    }

    public class SmartPlaylistSource : AbstractPlaylistSource
    {
        private static string generic_name = Catalog.GetString ("Smart Playlist");
        private static string properties_label = Catalog.GetString ("Edit Smart Playlist");
    
        private string order_by;
        private string order_dir;
        private string limit_number;
        private string limit_criterion;

#region Properties

        // Source override
        public override bool HasProperties {
            get { return true; }
        }

        public override bool CanRemoveTracks {
            get { return false; }
        }

        // AbstractPlaylistSource overrides
        protected override string SourceTable {
            get { return "CoreSmartPlaylists"; }
        }

        protected override string SourcePrimaryKey {
            get { return "SmartPlaylistID"; }
        }

        protected override string TrackJoinTable {
            get { return "CoreSmartPlaylistEntries"; }
        }

        protected override string IconName {
            get { return "source-smart-playlist"; }
        }

        // Custom properties
        private QueryNode condition;
        public QueryNode ConditionTree {
            get { return condition; }
            set {
                condition = value;
                if (condition != null) {
                    condition_sql = condition.ToSql (TrackListDatabaseModel.FieldSet);
                    condition_xml = condition.ToXml (TrackListDatabaseModel.FieldSet);
                }
            }
        }

        private string condition_sql;
        public string ConditionSql {
            get { return condition_sql; }
        }

        private string condition_xml;
        public string ConditionXml {
            get { return condition_xml; }
            set {
                condition_xml = value;
                ConditionTree = XmlQueryParser.Parse (condition_xml, TrackListDatabaseModel.FieldSet);
            }
        }

        public string OrderBy {
            get { return order_by; }
            set { order_by = value; }
        }

        public string OrderDir {
            get { return order_dir; }
            set { order_dir = value; }
        }

        public string Sort {
            get { return TrackListDatabaseModel.GetSort (OrderBy, OrderDir); }
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
                if (OrderBy == null || OrderBy == String.Empty)
                    return null;

                if (LimitCriterion == null || LimitCriterion == String.Empty)
                    return String.Format ("ORDER BY {0} LIMIT {1}", Sort, LimitNumber);
                else
                    return String.Format ("ORDER BY {0}", Sort);
            }
        }

        public bool PlaylistDependent {
            get { return false; }
        }

        public bool TimeDependent {
            get { return false; }
        }

#endregion

#region Constructors

        public SmartPlaylistSource (string name) : this (null, name, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty)
        {
        }

        /*public SmartPlaylistSource (SmartPlaylistSource original) : this (original.Name)
        {
            ConditionXml = original.ConditionXml;
            OrderBy = original.OrderBy;
            OrderDir = original.OrderDir;
            LimitNumber = original.LimitNumber;
            LimitCriterion = original.LimitCriterion;
        }*/

        // For existing smart playlists that we're loading from the database
        public SmartPlaylistSource (int? dbid, string name, string condition_xml, string order_by, string order_dir, string limit_number, string limit_criterion) :
            base (generic_name, name, dbid, -1, 0)
        {
            ConditionXml = condition_xml;
            OrderBy = order_by;
            OrderDir = order_dir;
            LimitNumber = limit_number;
            LimitCriterion = limit_criterion;
            DbId = dbid;

            Properties.SetString ("SourcePropertiesActionLabel", properties_label);

            //Globals.Library.TrackRemoved += OnLibraryTrackRemoved;

            //if (Globals.Library.IsLoaded)
                //OnLibraryReloaded(Globals.Library, new EventArgs());
            //else
                //Globals.Library.Reloaded += OnLibraryReloaded;

            //ListenToPlaylists();
        }

#endregion

#region Public Methods

        public void ListenToPlaylists ()
        {
        }

        public bool DependsOn (SmartPlaylistSource source)
        {
            return false;
        }


#endregion

#region AbstractPlaylist overrides

        protected override void Create ()
        {
            DbId = ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                INSERT INTO CoreSmartPlaylists
                    (Name, Condition, OrderBy, OrderDir, LimitNumber, LimitCriterion)
                    VALUES (?, ?, ?, ?, ?, ?)",
                Name, ConditionXml, OrderBy, OrderDir, LimitNumber, LimitCriterion
            ));
        }

        protected override void Update ()
        {
            ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                UPDATE CoreSmartPlaylists
                    SET Name = ?,
                        Condition = ?,
                        OrderBy = ?,
                        OrderDir = ?,
                        LimitNumber = ?,
                        LimitCriterion = ?
                    WHERE SmartPlaylistID = ?",
                Name, ConditionXml, OrderBy, OrderDir, LimitNumber, LimitCriterion, DbId
            ));
        }

#endregion

#region DatabaseSource overrides

        protected override void RateLimitedReload ()
        {
            // Wipe the member list clean
            ServiceManager.DbConnection.Execute (String.Format (
                "DELETE FROM CoreSmartPlaylistEntries WHERE SmartPlaylistID = {0}", DbId
            ));

            // Repopulate it 
            ServiceManager.DbConnection.Execute (String.Format (
                @"INSERT INTO CoreSmartPlaylistEntries 
                    SELECT {0} as SmartPlaylistID, TrackId
                        FROM CoreTracks, CoreArtists, CoreAlbums
                        WHERE CoreTracks.ArtistID = CoreArtists.ArtistID AND CoreTracks.AlbumID = CoreAlbums.AlbumID
                        {1} {2}",
                DbId, PrependCondition("AND"), OrderAndLimit
            ));

            base.RateLimitedReload ();
        }

#endregion

        private string PrependCondition (string with)
        {
            return String.IsNullOrEmpty (ConditionSql) ? " " : String.Format ("{0} ({1})", with, ConditionSql);
        }

        private static Order [] orders = new Order [] {
            new Order ("Random",    Catalog.GetString("Random"), null),
            new Order ("Album",     Catalog.GetString("Album"), "ASC"),
            new Order ("Artist",    Catalog.GetString("Artist"), "ASC"),
            new Order ("Genre",     Catalog.GetString("Genre"), "ASC"),
            new Order ("Title",     Catalog.GetString("Title"), "ASC"),
            //--
            new Order ("Rating",    Catalog.GetString("Highest Rating"), "DESC"),
            new Order ("Rating",    Catalog.GetString("Lowest Rating"), "ASC"),
            //--
            new Order ("PlayCount", Catalog.GetString("Least Played"), "ASC"),
            new Order ("PlayCount", Catalog.GetString("Most Played"), "DESC"),
            //--
            new Order ("DateAddedStamp", Catalog.GetString("Most Recently Added"), "DESC"),
            new Order ("DateAddedStamp", Catalog.GetString("Least Recently Added"), "ASC"),
            //--
            new Order ("LastPlayedStamp", Catalog.GetString("Most Recently Played"), "DESC"),
            new Order ("LastPlayedStamp", Catalog.GetString("Least Recently Played"), "ASC")
        };

        public static Order [] Orders {
            get { return orders; }
        }

        public static List<SmartPlaylistSource> LoadAll ()
        {
            List<SmartPlaylistSource> sources = new List<SmartPlaylistSource> ();

            using (IDataReader reader = ServiceManager.DbConnection.ExecuteReader (
                "SELECT SmartPlaylistID, Name, Condition, OrderBy, OrderDir, LimitNumber, LimitCriterion FROM CoreSmartPlaylists")) {
                while (reader.Read ()) {
                    try {
                        SmartPlaylistSource playlist = new SmartPlaylistSource (
                            Convert.ToInt32 (reader[0]), reader[1] as string,
                            reader[2] as string, reader[3] as string,
                            reader[4] as string, reader[5] as string,
                            reader[6] as string
                        );
                        sources.Add (playlist);
                    } catch (Exception e) {
                        Log.Warning ("Ignoring Smart Playlist", String.Format ("Caught error: {0}", e), false);
                    }
                }
            }
            
            return sources;
        }
    }
}
