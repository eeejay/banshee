//
// Item.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;

using Mono.Unix;

using Hyena.Collections;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.Database;
using Banshee.Gui;
using Banshee.Library;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Playlist;
using Banshee.Preferences;
using Banshee.ServiceStack;
using Banshee.Sources;

using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class Item
    {
        public static Item LoadOrCreate (string id, string title, string mediaType)
        {
            var item = Provider.FetchFirstMatching ("ID = ?", id);
            if (item == null) {
                item = new Item (id, title, mediaType);
                item.Save ();
            }

            return item;
        }

        public static IEnumerable<Item> LoadAll ()
        {
            return Provider.FetchAll ();
        }

        [DatabaseColumn("ItemId", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private int DbId { get; set; }

        [DatabaseColumn]
        private string DetailsJson { get; set; }

        [DatabaseColumn]
        public string Id { get; private set; }

        [DatabaseColumn]
        public string Title { get; private set; }

        [DatabaseColumn]
        public string MediaType { get; private set; }

        [DatabaseColumn]
        public string SelectedFormat { get; set; }

        [DatabaseColumn]
        public string BookmarkFile { get; set; }

        [DatabaseColumn]
        public int BookmarkPosition { get; set; }

        public IA.Details Details { get; private set; }

        public Item () {}

        private Item (string id, string title, string mediaType)
        {
            Id = id;
            Title = title;
            MediaType = mediaType;
        }

        public void Delete ()
        {
            Provider.Delete (this);
        }

        public void LoadDetails ()
        {
            if (Details == null) {
                Details = new IA.Details (Id, DetailsJson);
                DetailsJson = Details.Json;
                Save ();
            }
        }

        public void Save ()
        {
            Provider.Save (this);
        }

        private static SqliteModelProvider<Item> provider;
        private static SqliteModelProvider<Item> Provider {
            get {
                if (provider == null) {
                    provider = new SqliteModelProvider<Item> (ServiceManager.DbConnection, "IaItems", false);
                }
                return provider;
            }
        }

        static Item () {
            var db = ServiceManager.DbConnection;
            if (!db.TableExists ("IaItems")) {
                db.Execute (@"
                    CREATE TABLE IaItems (
                        ItemID         INTEGER PRIMARY KEY,
                        ID             TEXT UNIQUE NOT NULL,
                        Title          TEXT NOT NULL,
                        MediaType      TEXT,
                        DetailsJson    TEXT,

                        SelectedFormat TEXT,
                        BookmarkFile   TEXT,
                        BookmarkPosition INTEGER DEFAULT 0
                    )"
                );
            }
        }
    }
}
