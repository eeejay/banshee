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

using Hyena.Json;

using InternetArchive;
using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class Item
    {
        JsonObject details;
        JsonObject metadata, misc, item, review_info;
        JsonArray reviews;

        public static Item LoadOrCreate (string id, string title)
        {
            /*var item = Provider.LoadFromId (id);
            if (item != null)
                return item;*/

            return new Item (id, title);
        }

        private Item (string id, string title)
        {
            Id = id;
            Title = title;
            //Provider.Save ();

            LoadDetails ();
        }

#region Properties stored in database columns

        //[DatabaseColumn (PrimaryKey=true)]
        public long DbId { get; set; }

        //[DatabaseColumn]
        public string Id { get; set; }

        //[DatabaseColumn]
        public string Title { get; private set; }

        //[DatabaseColumn]
        public string JsonDetails { get; set; }

        //[DatabaseColumn]
        public bool IsHidden { get; set; }

#endregion

#region Properties from the JSON object

        public string Description {
            get { return metadata.GetJoined ("description", System.Environment.NewLine); }
        }

        public string Creator {
            get { return metadata.GetJoined ("creator", ", "); }
        }

        public string Publisher {
            get { return metadata.GetJoined ("publisher", ", "); }
        }

        public string Year {
            get { return metadata.GetJoined ("year", ", "); }
        }

        public string Subject {
            get { return metadata.GetJoined ("subject", ", "); }
        }

        public string Source {
            get { return metadata.GetJoined ("source", ", "); }
        }

        public string Taper {
            get { return metadata.GetJoined ("taper", ", "); }
        }

        public string Lineage {
            get { return metadata.GetJoined ("lineage", ", "); }
        }

        public string Transferer {
            get { return metadata.GetJoined ("transferer", ", "); }
        }

        public DateTime DateAdded {
            get { return GetMetadataDate ("addeddate"); }
        }

        public string AddedBy {
            get { return metadata.GetJoined ("adder", ", "); }
        }

        public string Venue {
            get { return metadata.GetJoined ("venue", ", "); }
        }

        public string Coverage {
            get { return metadata.GetJoined ("coverage", ", "); }
        }

        public string ImageUrl {
            get { return misc.Get<string> ("image"); }
        }

        public long DownloadsAllTime {
            get { return (int)item.Get<double> ("downloads"); }
        }

        public long DownloadsLastMonth {
            get { return (int)item.Get<double> ("month"); }
        }

        public long DownloadsLastWeek {
            get { return (int)item.Get<double> ("week"); }
        }

        public DateTime DateCreated {
            get { return GetMetadataDate ("date"); }
        }

        private DateTime GetMetadataDate (string key)
        {
            DateTime ret;
            if (DateTime.TryParse (metadata.GetJoined (key, null), out ret))
                return ret;
            else
                return DateTime.MinValue;
        }

        public IEnumerable<File> Files {
            get {
                string location_root = String.Format ("http://{0}{1}", details.Get<string> ("server"), details.Get<string> ("dir"));
                var files = details["files"] as JsonObject;
                foreach (JsonObject file in files.Values) {
                    yield return new File (file, location_root);
                }
            }
        }

        public IEnumerable<Review> Reviews {
            get {
                if (reviews == null) {
                    yield break;
                }

                foreach (JsonObject review in reviews) {
                    yield return new Review (review);
                }
            }
        }

        public double AvgRating {
            get { return review_info.Get<double> ("avg_rating"); }
        }

        public int NumReviews {
            get { return review_info.Get<int> ("num_reviews"); }
        }

#endregion

        private bool LoadDetails ()
        {
            // First see if we already have the Hyena.JsonObject parsed
            if (details != null) {
                return true;
            }

            // If not, see if we have a copy in the database, and parse that
            /*if (JsonDetails != null) {
                details = new Hyena.Json.Deserializer (JsonDetails).Deserialize () as JsonObject;
                return true;
            }*/


            // Hack to load JSON data from local file instead of from archive.org
            if (Id == "banshee-internet-archive-offline-mode") {
                details = new Hyena.Json.Deserializer (System.IO.File.ReadAllText ("item2.json")).Deserialize () as JsonObject;
            } else {
                // We don't; grab it from archive.org and parse it
                string json_str = IA.Item.GetDetails (Id);

                if (json_str != null) {
                    details = new Hyena.Json.Deserializer (json_str).Deserialize () as JsonObject;
                    JsonDetails = json_str;
                }
            }

            if (details != null) {
                metadata = details.Get<JsonObject> ("metadata");
                misc     = details.Get<JsonObject> ("misc");
                item     = details.Get<JsonObject> ("item");
                var r    = details.Get<JsonObject> ("reviews");
                if (r != null) {
                    reviews = r.Get<JsonArray> ("reviews");
                    review_info = r.Get<JsonObject> ("info");
                }
            }

            return false;
        }
    }
}
