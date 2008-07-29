//
// PodcastTrackListModel.cs
//
// Authors:
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

using Gtk;
using Gdk;

using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Data.Sqlite;

using Banshee.Gui;
using Banshee.Base;
using Banshee.Database;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Collection.Database;

using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.Podcasting.Data;

using Migo.Syndication;

namespace Banshee.Podcasting.Gui
{
    public class PodcastTrackListModel : DatabaseTrackListModel, IListModel<PodcastTrackInfo>
    {
        public PodcastTrackListModel (BansheeDbConnection conn, IDatabaseTrackModelProvider provider, DatabaseSource source) : base (conn, provider, source)
        {
            JoinTable = String.Format ("{0}, {1}, {2}", Feed.Provider.TableName, FeedItem.Provider.TableName, FeedEnclosure.Provider.TableName);
            JoinPrimaryKey = FeedItem.Provider.PrimaryKey;
            JoinColumn = "ExternalID";
            AddCondition (String.Format (
                "{0}.FeedID = {1}.FeedID AND CoreTracks.ExternalID = {1}.ItemID AND {1}.ItemID = {2}.ItemID",
                Feed.Provider.TableName, FeedItem.Provider.TableName, FeedEnclosure.Provider.TableName
            ));
        }

        protected override void GenerateSortQueryPart ()
        {
            SortQuery = (SortColumn == null)
                ? GetSort ("Published", false)
                : GetSort (SortColumn.SortKey, SortColumn.SortType == Hyena.Data.SortType.Ascending);
        }
        
        protected override void UpdateUnfilteredAggregates ()
        {
            HyenaSqliteCommand count_command = new HyenaSqliteCommand (String.Format (
                "SELECT COUNT(*) {0} AND PodcastItems.IsRead = 0", UnfilteredQuery
            ));
            UnfilteredCount = Connection.Query<int> (count_command);
        }

        public static string GetSort (string key, bool asc)
        {
            string ascDesc = asc ? "ASC" : "DESC";
            string sort_query = null;
            switch(key) {
                case "PublishedDate":
                    sort_query = String.Format (@"
                        PodcastItems.PubDate {0}", ascDesc);
                    break;

                case "DownloadStatus":
                    sort_query = String.Format (@"
                        PodcastEnclosures.DownloadStatus {0}", ascDesc);
                    break;
            }

            return sort_query ?? Banshee.Query.BansheeQuery.GetSort (key, asc);
        }
        
        public new PodcastTrackInfo this[int index] {
            get {
                lock (this) {
                    return cache.GetValue (index) as PodcastTrackInfo;
                }
            }
        }
    }
}
