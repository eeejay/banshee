//
// Search.cs
//  
// Author:
//       Gabriel Burt <gabriel.burt@gmail.com>
// 
// Copyright (c) 2009 Gabriel Burt
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;

namespace InternetArchive
{
    public class Search
    {
        List<Sort> sorts = new List<Sort> ();
        List<Field> result_fields = new List<Field> ();
        int NumResults;

        public IList<Field>  ReturnFields { get { return result_fields; } }
        public IList<Sort>   Sorts { get { return sorts; } }
        public ResultsFormat Format { get; set; }
        public string Query { get; set; }

        static Search () {
            UserAgent = "InternetArchiveSharp";
            TimeoutMs = 20000;
        }

        public Search ()
        {
            NumResults = 50;

            result_fields.AddRange (new Field [] {
                Field.AvgRating,
                Field.Creator,
                Field.Date,
                Field.Description,
                Field.Downloads,
                Field.Format,
                Field.Identifier,
                Field.Language,
                Field.LicenseUrl,
                Field.MediaType,
                Field.NumReviews,
                Field.Publisher,
                Field.Title,
                Field.Year
            });

            sorts.Add (new Sort () { Id = "avg_rating desc" });
        }

        private string GetQuery ()
        {
            var sb = new System.Text.StringBuilder ();

            sb.AppendFormat ("q={0}", System.Web.HttpUtility.UrlEncode (Query));

            foreach (var field in result_fields) {
                sb.AppendFormat ("&fl[]={0}", System.Web.HttpUtility.UrlEncode (field.Id));
            }

            foreach (var sort in sorts) {
                sb.AppendFormat ("&sort[]={0}", System.Web.HttpUtility.UrlEncode (sort.Id));
            }

            sb.AppendFormat ("&rows={0}", NumResults);
            sb.AppendFormat ("&fmt={0}", Format.Id);
            sb.Append ("&xmlsearch=Search");

            return sb.ToString ();
        }

        public string GetResults ()
        {
            HttpWebResponse response = null;
            string url = null;

            try {
                url = String.Format ("http://www.archive.org/advancedsearch.php?{0}", GetQuery ());
                Hyena.Log.Debug ("ArchiveSharp Searching", url);

                var request = (HttpWebRequest) WebRequest.Create (url);
                request.UserAgent = UserAgent;
                request.Timeout   = TimeoutMs;
                request.KeepAlive = KeepAlive;

                response = (HttpWebResponse) request.GetResponse ();

                if (response.StatusCode != HttpStatusCode.OK) {
                    return null;
                }

                using (Stream stream = response.GetResponseStream ()) {
                    using (StreamReader reader = new StreamReader (stream)) {
                        return reader.ReadToEnd ();
                    }
                }
            } finally {
                if (response != null) {
                    if (response.StatusCode != HttpStatusCode.OK) {
                        Hyena.Log.WarningFormat ("Got status {0} searching {1}", response.StatusCode, url);
                    }
                    response.Close ();
                    response = null;
                }
            }
        }

        public static string UserAgent { get; set; }
        public static bool   KeepAlive { get; set; }
        public static int    TimeoutMs { get; set; }
    }
}
