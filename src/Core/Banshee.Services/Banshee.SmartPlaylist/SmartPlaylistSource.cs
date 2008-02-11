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

using Hyena.Query;
using Hyena.Data.Sqlite;
 
using Banshee.Base;
using Banshee.Query;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Playlist;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.SmartPlaylist
{
    public class SmartPlaylistSource : AbstractPlaylistSource, IUnmapableSource
    {
        private static string generic_name = Catalog.GetString ("Smart Playlist");
        private static string properties_label = Catalog.GetString ("Edit Smart Playlist");
    
        private QueryOrder query_order;
        private QueryLimit limit;
        private IntegerQueryValue limit_value;

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

        // Custom properties
        private QueryNode condition;
        public QueryNode ConditionTree {
            get { return condition; }
            set {
                condition = value;
                if (condition != null) {
                    condition_sql = condition.ToSql (BansheeQuery.FieldSet);
                    condition_xml = condition.ToXml (BansheeQuery.FieldSet);
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
                ConditionTree = XmlQueryParser.Parse (condition_xml, BansheeQuery.FieldSet);
            }
        }

        public QueryOrder QueryOrder {
            get { return query_order; }
            set { query_order = value; }
        }

        public IntegerQueryValue LimitValue {
            get { return limit_value; }
            set { limit_value = value; }
        }

        public QueryLimit Limit {
            get { return limit; }
            set { limit = value; }
        }

        protected string OrderAndLimit {
            get {
                if (IsLimited) {
                    return String.Format ("{0} {1}", QueryOrder.ToSql (), Limit.ToSql (LimitValue));
                } else {
                    return null;
                }
            }
        }

        public bool IsLimited {
            get {
                return (Limit != null && LimitValue != null && !LimitValue.IsEmpty && QueryOrder != null);
            }
        }

        // FIXME scan ConditionTree for playlist fields
        public bool PlaylistDependent {
            get { return false; }
        }

        // FIXME scan ConditionTree for date fields
        public bool TimeDependent {
            get { return false; }
        }

#endregion

#region Constructors

        public SmartPlaylistSource (string name) : this (null, name, String.Empty, String.Empty, String.Empty, String.Empty)
        {
        }

        public SmartPlaylistSource (string name, QueryNode condition, QueryOrder order, QueryLimit limit, IntegerQueryValue limit_value)
            : base (generic_name, name, null, -1, 0)
        {
            ConditionTree = condition;
            QueryOrder = order;
            Limit = limit;
            LimitValue = limit_value;

            InstallProperties ();
        }

        // For existing smart playlists that we're loading from the database
        public SmartPlaylistSource (int? dbid, string name, string condition_xml, string order_by, string limit_number, string limit_criterion) :
            base (generic_name, name, dbid, -1, 0)
        {
            ConditionXml = condition_xml;
            QueryOrder = BansheeQuery.FindOrder (order_by);
            
            Limit = BansheeQuery.FindLimit (limit_criterion);

            LimitValue = new IntegerQueryValue ();
            LimitValue.ParseUserQuery (limit_number);

            Console.WriteLine ("limit = {0}, order = {1}, val = {2}, valisempty? {3}", Limit, QueryOrder, LimitValue, LimitValue.IsEmpty);

            DbId = dbid;

            InstallProperties ();

            //Globals.Library.TrackRemoved += OnLibraryTrackRemoved;

            //if (Globals.Library.IsLoaded)
                //OnLibraryReloaded(Globals.Library, new EventArgs());
            //else
                //Globals.Library.Reloaded += OnLibraryReloaded;

            //ListenToPlaylists();
        }

        protected void InstallProperties ()
        {
            Properties.SetString ("IconName", "source-smart-playlist");
            Properties.SetString ("SourcePropertiesActionLabel", properties_label);
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Delete Smart Playlist"));
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
                    (Name, Condition, OrderBy, LimitNumber, LimitCriterion)
                    VALUES (?, ?, ?, ?, ?)",
                Name, ConditionXml,
                IsLimited ? QueryOrder.Name : null,
                IsLimited ? LimitValue.ToSql () : null,
                IsLimited ? Limit.Name : null
            ));
        }

        protected override void Update ()
        {
            ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                UPDATE CoreSmartPlaylists
                    SET Name = ?,
                        Condition = ?,
                        OrderBy = ?,
                        LimitNumber = ?,
                        LimitCriterion = ?
                    WHERE SmartPlaylistID = ?",
                Name, ConditionXml,
                IsLimited ? QueryOrder.Name : null,
                IsLimited ? LimitValue.ToSql () : null,
                IsLimited ? Limit.Name : null,
                DbId
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

            Console.WriteLine ("limited? {0}", IsLimited);

            Console.WriteLine (String.Format (
                @"INSERT INTO CoreSmartPlaylistEntries 
                    SELECT {0} as SmartPlaylistID, TrackId
                        FROM CoreTracks, CoreArtists, CoreAlbums
                        WHERE CoreTracks.ArtistID = CoreArtists.ArtistID AND CoreTracks.AlbumID = CoreAlbums.AlbumID
                        {1} {2}",
                DbId, PrependCondition("AND"), OrderAndLimit
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

#region IUnmapableSource Implementation

        public bool Unmap ()
        {
            if (DbId != null) {
                ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                    BEGIN TRANSACTION;
                        DELETE FROM CoreSmartPlaylists WHERE SmartPlaylistID = ?;
                        DELETE FROM CoreSmartPlaylistEntries WHERE SmartPlaylistID = ?;
                    COMMIT TRANSACTION",
                    DbId, DbId
                ));
            }

            Remove ();
            return true;
        }

        public bool CanUnmap {
            get { return true; }
        }

        public bool ConfirmBeforeUnmap {
            get { return true; }
        }

#endregion

        private string PrependCondition (string with)
        {
            return String.IsNullOrEmpty (ConditionSql) ? " " : String.Format ("{0} ({1})", with, ConditionSql);
        }

        public static List<SmartPlaylistSource> LoadAll ()
        {
            List<SmartPlaylistSource> sources = new List<SmartPlaylistSource> ();

            using (IDataReader reader = ServiceManager.DbConnection.ExecuteReader (
                "SELECT SmartPlaylistID, Name, Condition, OrderBy, LimitNumber, LimitCriterion FROM CoreSmartPlaylists")) {
                while (reader.Read ()) {
                    try {
                        SmartPlaylistSource playlist = new SmartPlaylistSource (
                            Convert.ToInt32 (reader[0]), reader[1] as string,
                            reader[2] as string, reader[3] as string,
                            reader[4] as string, reader[5] as string
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
