//
// TrackListDatabaseModel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
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
using System.Text;
using System.Collections.Generic;

using Mono.Unix;
using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Data.Query;

using Banshee.Base;
using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class TrackListDatabaseModel : TrackListModel, IExportableModel, 
        ICacheableDatabaseModel<LibraryTrackInfo>, IFilterable, ISortable, ICareAboutView
    {
        private BansheeDbConnection connection;
        private BansheeModelProvider<LibraryTrackInfo> provider;
        private BansheeDatabaseModelCache<LibraryTrackInfo> cache;
        private int count;
        private int unfiltered_count;
        
        private ISortableColumn sort_column;
        private string sort_query;
        
        private string reload_fragment;
        private string join_fragment, condition;

        private string filter_query;
        private string filter;
        private string artist_id_filter_query;
        private string album_id_filter_query;
        
        private int rows_in_view;

        public TrackListDatabaseModel (BansheeDbConnection connection, string uuid)
        {
            this.connection = connection;
            provider = LibraryTrackInfo.Provider;
            cache = new BansheeDatabaseModelCache <LibraryTrackInfo> (connection, uuid, this);
            Refilter ();
        }
        
        private void GenerateFilterQueryPart()
        {
            if (String.IsNullOrEmpty(Filter)) {
                filter_query = null;
            } else {
                Hyena.Data.Query.UserQueryParser qp = new UserQueryParser (Filter);
                QueryNode n = qp.BuildTree ();

                filter_query = n.ToSql (field_set);

                Console.WriteLine ("query: {0}", Filter);
                //Console.WriteLine ("tree:");
                //n.Dump ();
                Console.WriteLine ("Xml for Query: {0}", n.ToXml ());
                Console.WriteLine ("Sql for Query: {0}", filter_query);
                Hyena.Data.Query.QueryParser qp2 = new XmlQueryParser (n.ToXml ());
                QueryNode n2 = qp2.BuildTree ();
                if (n2 != null) {
                    Console.WriteLine ("User query for Xml: {0}", n2.ToUserQuery ());
                } else
                    Console.WriteLine ("n2 is null");

                if (filter_query.Length == 0)
                    filter_query = null;
            }
        }
        
        private string AscDesc ()
        {
            return sort_column.SortType == SortType.Ascending ? " ASC" : " DESC";
        }

        private void GenerateSortQueryPart()
        {
            if(sort_column == null) {
                sort_query = null;
                return;
            }

            sort_query = GetSort (sort_column.SortKey, AscDesc ());
        }

        private const string default_sort = "lower(CoreArtists.Name) ASC, lower(CoreAlbums.Title) ASC, CoreTracks.TrackNumber ASC, CoreTracks.Uri ASC";
        public static string GetSort (string key, string ascDesc)
        {
            string sort_query = null;
            switch(key) {
                case "Track":
                    sort_query = String.Format (@"
                        lower(CoreArtists.Name) ASC, 
                        lower(CoreAlbums.Title) ASC, 
                        CoreTracks.TrackNumber {0}", ascDesc); 
                    break;

                case "Artist":
                    sort_query = String.Format (@"
                        lower(CoreArtists.Name) {0}, 
                        lower(CoreAlbums.Title) ASC, 
                        CoreTracks.TrackNumber ASC,
                        CoreTracks.Uri ASC", ascDesc); 
                    break;

                case "Album":
                    sort_query = String.Format (@"
                        lower(CoreAlbums.Title) {0},
                        lower(CoreArtists.Name) ASC,
                        CoreTracks.TrackNumber ASC,
                        CoreTracks.Uri ASC", ascDesc); 
                    break;

                case "Title":
                    sort_query = String.Format (@"
                        lower(CoreTracks.Title) {0},
                        lower(CoreArtists.Name) ASC, 
                        lower(CoreAlbums.Title) ASC", ascDesc); 
                    break;

                case "Random":
                    sort_query = "RANDOM ()";
                    break;

                case "Year":
                case "Duration":
                case "Rating":
                case "PlayCount":
                case "SkipCount":
                case "LastPlayedStamp":
                case "DateAddedStamp":
                case "Uri":
                    sort_query = String.Format (
                        "CoreTracks.{0} {1}, {2}",
                        key, ascDesc, default_sort
                    );
                    break;
            }
            return sort_query;
        }

        public void Refilter()
        {
            lock(this) {
                GenerateFilterQueryPart();
                cache.Clear ();
            }
        }
        
        public void Sort(ISortableColumn column)
        {
            lock(this) {
                if(sort_column == column && sort_column != null) {
                    sort_column.SortType = sort_column.SortType == SortType.Ascending 
                        ? SortType.Descending 
                        : SortType.Ascending;
                }
            
                sort_column = column;
            
                GenerateSortQueryPart();
                cache.Clear ();
            }
        }
        
        public override void Clear()
        {
            cache.Clear ();
            unfiltered_count = 0;
            count = 0;
            OnCleared();
        }
        
        private bool first_reload = true;
        public override void Reload()
        {
            string unfiltered_query = String.Format (
                "FROM {0}{1} WHERE {2} {3}",
                From, JoinFragment, Where, ConditionFragment
            );

            unfiltered_count = connection.QueryInt32 (String.Format (
                "SELECT COUNT(*) {0}", unfiltered_query
            ));

            StringBuilder qb = new StringBuilder ();
                
            qb.Append (unfiltered_query);
            
            if (artist_id_filter_query != null) {
                qb.Append ("AND ");
                qb.Append (artist_id_filter_query);
            }
                    
            if (album_id_filter_query != null) {
                qb.Append ("AND ");
                qb.Append (album_id_filter_query);
            }
            
            if (filter_query != null) {
                qb.Append ("AND ");
                qb.Append (filter_query);
            }
            
            if (sort_query != null) {
                qb.Append (" ORDER BY ");
                qb.Append (sort_query);
            }
                
            reload_fragment = qb.ToString ();

            if (!first_reload || !cache.Warm) {
                cache.Reload ();
            }

            count = cache.Count;
            first_reload = false;

            OnReloaded ();
        }

        public override int IndexOf (TrackInfo track)
        {
            if (track is LibraryTrackInfo) {
                return ((LibraryTrackInfo)track).DbIndex;
            }
            
            return -1;
        }

        public override TrackInfo this[int index] {
            get { return cache.GetValue (index); }
        }
        
        public override int Count {
            get { return count; }
        }
        
        public int UnfilteredCount {
            get { return unfiltered_count; }
        }
        
        public string Filter {
            get { return filter; }
            set { 
                lock(this) {
                    filter = value; 
                }
            }
        }

        public string JoinFragment {
            get { return join_fragment; }
            set {
                join_fragment = value;
                Refilter();
            }
        }

        public string Condition {
            get { return condition; }
            set {
                condition = value;
                Refilter();
            }
        }

        public string ConditionFragment {
            get { return PrefixCondition ("AND"); }
        }

        private string PrefixCondition (string prefix)
        {
            string condition = Condition;
            if (condition == null || condition == String.Empty)
                return String.Empty;
            else
                return String.Format (" {0} {1} ", prefix, condition);
        }
        
        public override IEnumerable<ArtistInfo> ArtistInfoFilter {
            set { 
                ModelHelper.BuildIdFilter<ArtistInfo>(value, "CoreTracks.ArtistID", artist_id_filter_query,
                    delegate(ArtistInfo artist) {
                        if(!(artist is LibraryArtistInfo)) {
                            return null;
                        }
                        
                        return ((LibraryArtistInfo)artist).DbId.ToString();
                    },
                
                    delegate(string new_filter) {
                        artist_id_filter_query = new_filter;
                        Refilter();
                        Reload();
                    }
                );
            }
        }
        
        public override IEnumerable<AlbumInfo> AlbumInfoFilter {
            set { 
                ModelHelper.BuildIdFilter<AlbumInfo>(value, "CoreTracks.AlbumID", album_id_filter_query,
                    delegate(AlbumInfo album) {
                        if(!(album is LibraryAlbumInfo)) {
                            return null;
                        }
                        
                        return ((LibraryAlbumInfo)album).DbId.ToString();
                    },
                
                    delegate(string new_filter) {
                        album_id_filter_query = new_filter;
                        Refilter();
                        Reload();
                    }
                );
            }
        }

        public int CacheId {
            get { return cache.CacheId; }
        }

        public ISortableColumn SortColumn { 
            get { return sort_column; }
        }
                
        public virtual int RowsInView {
            protected get { return rows_in_view; }
            set { rows_in_view = value; }
        }

        int IExportableModel.GetLength() 
        {
            return Count;
        }
        
        IDictionary<string, object> IExportableModel.GetMetadata(int index)
        {
            return this[index].GenerateExportable();
        }

        public BansheeModelProvider<LibraryTrackInfo> Provider {
            get { return provider; }
        }

        // Implement ICacheableModel
        public int FetchCount {
            get { return RowsInView > 0 ? RowsInView * 5 : 100; }
        }

        // Implement IDatabaseModel
        public LibraryTrackInfo GetItemFromReader (IDataReader reader, int index)
        {
            LibraryTrackInfo track = new LibraryTrackInfo (index);
            Provider.Load (track, reader);
            return track;
        }
        
        public string PrimaryKey {
            get { return provider.PrimaryKey; }
        }

        public string ReloadFragment {
            get { return reload_fragment; }
        }

        public string Select {
            get { return provider.Select; }
        }

        public string From {
            get { return provider.From; }
        }

        public string Where {
            get { return provider.Where; }
        }

        public override QueryField ArtistField {
            get { return field_set.Fields [0]; }
        }

        public override QueryField AlbumField {
            get { return field_set.Fields [1]; }
        }

        protected static QueryFieldSet field_set = new QueryFieldSet (
            new QueryField (
                Catalog.GetString ("Artist"), "CoreArtists.Name", QueryFieldType.Text, true,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("by"), Catalog.GetString ("artist"), Catalog.GetString ("artists")
            ),
            new QueryField (
                Catalog.GetString ("Album"), "CoreAlbums.Title", QueryFieldType.Text, true,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("on"), Catalog.GetString ("album"), Catalog.GetString ("from")
            ),
            new QueryField (
                Catalog.GetString ("Track Title"), "CoreTracks.Title", QueryFieldType.Text, true,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("title"), Catalog.GetString ("titled")
            ),
            new QueryField (
                Catalog.GetString ("Year"), "CoreTracks.Year", QueryFieldType.Numeric,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("year"), Catalog.GetString ("released")
            ),
            new QueryField (
                Catalog.GetString ("Rating"), "CoreTracks.Rating", QueryFieldType.Numeric,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("rating"), Catalog.GetString ("stars")
            ),
            new QueryField (
                Catalog.GetString ("Play Count"), "CoreTracks.PlayCount", QueryFieldType.Numeric,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("plays"), Catalog.GetString ("playcount")
            ),
            new QueryField (
                Catalog.GetString ("Skip Count"), "CoreTracks.SkipCount", QueryFieldType.Numeric,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("skips"), Catalog.GetString ("skipcount")
            ),
            new QueryField (
                Catalog.GetString ("File Size"), "CoreTracks.FileSize", QueryFieldType.Numeric, FileSizeModifier,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("size"), Catalog.GetString ("filesize")
            ),
            new QueryField (
                Catalog.GetString ("File Path"), "CoreTracks.Uri", QueryFieldType.Text,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("path"), Catalog.GetString ("file"), Catalog.GetString ("uri")
            ),
            new QueryField (
                Catalog.GetString ("Mime Type"), "CoreTracks.MimeType", QueryFieldType.Text,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("type"), Catalog.GetString ("mimetype"), Catalog.GetString ("format")
            ),
            new QueryField (
                Catalog.GetString ("Last Played Date"), "CoreTracks.LastPlayedStamp", QueryFieldType.Numeric, DateTimeModifier,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("lastplayed"), Catalog.GetString ("played")
            ),
            new QueryField (
                Catalog.GetString ("Imported Date"), "CoreTracks.DateAddedStamp", QueryFieldType.Numeric, DateTimeModifier,
                // Translators: These are search fields.  Please, no spaces. Duplicates ok.
                Catalog.GetString ("addedon"), Catalog.GetString ("dateadded"), Catalog.GetString ("importedon")
            )
        );
        
        private static string FileSizeModifier (string input)
        {
            long value = 0;
            
            if (input.Length < 2) {
                return input;
            } else if (input[input.Length - 1] == 'b' || input[input.Length - 1] == 'B') {
                input = input.Substring (0, input.Length - 1);
            }
            
            if (!Int64.TryParse (input.Substring (0, input.Length - 1), out value)) {
                return input;
            }
            
            switch (input[input.Length - 1]) {
                case 'k': case 'K': value *= 1024; break;
                case 'm': case 'M': value *= 1048576; break;
                case 'g': case 'G': value *= 1073741824; break;
                case 't': case 'T': value *= 1099511627776; break;
                default: break;
            }
            
            return Convert.ToString (value);
        }
        
        private static string DateTimeModifier (string input)
        {
            // TODO: Add support for relative strings like "yesterday", "3 weeks ago", "5 days ago"
            try {
                return DateTimeUtil.ToTimeT (DateTime.Parse (input)).ToString ();
            } catch {
                return "0";
            }
        }
    }
}
