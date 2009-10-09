//
// Item.cs
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
    public abstract class Item
    {
        public Item ()
        {
        }

        public string GetJoined (Field field, string with)
        {
            var ary = Get<System.Collections.IEnumerable> (field);
            if (ary != null) {
                return String.Join (with, ary.Cast<object> ().Select (o => o.ToString ()).ToArray ());
            }

            return null;
        }

        public string Id {
            get { return Get<string> (Field.Identifier); }
        }

        public string WebpageUrl {
            get { return String.Format ("http://www.archive.org/details/{0}", Id); }
        }

        public static string GetDetails (string id)
        {
            HttpWebResponse response = null;
            string url = String.Format ("http://www.archive.org/details/{0}&output=json", id);

            try {
                Hyena.Log.Debug ("ArchiveSharp Getting Details", url);

                var request = (HttpWebRequest) WebRequest.Create (url);
                request.UserAgent = Search.UserAgent;
                request.Timeout   = Search.TimeoutMs;
                request.KeepAlive = Search.KeepAlive;

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

        public abstract T Get<T> (Field field);
    }
}
