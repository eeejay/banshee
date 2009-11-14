//
// LastfmData.cs
//
// Authors:
//   Gabriel Burt
//   Fredrik Hedberg
//   Aaron Bockover
//   Lukas Lipka
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
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.GZip;

using Hyena;

namespace Lastfm.Data
{
    public enum CacheDuration
    {
        None = 0,
        Normal,
        Infinite
    }

    public class LastfmData<T> : IEnumerable<T> where T : DataEntry
    {
        private static DataEntryCollection<T> empty_collection = new DataEntryCollection<T> ((XmlNodeList)null);
        protected DataEntryCollection<T> collection = empty_collection;
        protected XmlDocument doc;
        protected string data_url;
        protected string cache_file;
        protected string xpath;
        protected CacheDuration cache_duration;

        public LastfmData (string dataUrlFragment) : this (dataUrlFragment, CacheDuration.Normal, null)
        {
        }

        public LastfmData (string dataUrlFragment, string xpath) : this (dataUrlFragment, CacheDuration.Normal, xpath)
        {
        }

        public LastfmData (string dataUrlFragment, CacheDuration cacheDuration) : this (dataUrlFragment, cacheDuration, null)
        {
        }

        public LastfmData (string dataUrlFragment, CacheDuration cacheDuration, string xpath)
        {
            DataCore.Initialize ();

            this.data_url = DataCore.FixLastfmUrl (String.Format ("http://ws.audioscrobbler.com/1.0/{0}", dataUrlFragment));
            this.cache_file = DataCore.GetCachedPathFromUrl (data_url);
            this.cache_duration = cacheDuration;
            this.xpath = xpath;

            try {
                GetData ();
            } catch {
                Refresh ();
            }
        }

        public void Refresh ()
        {
            CacheDuration old_duration = cache_duration;
            cache_duration = CacheDuration.None;
            GetData ();
            cache_duration = old_duration;
        }

        private void GetData ()
        {
            // Download the content if necessary
            DataCore.DownloadContent (data_url, cache_file, cache_duration);

            // Load the XML from the new or cached local file
            doc = new XmlDocument ();

            XmlReaderSettings settings = new XmlReaderSettings ();
            settings.CheckCharacters = false;

            using (XmlReader reader = XmlReader.Create (cache_file, settings)) {
                doc.Load (reader);
            }

            if (xpath == null) {
                collection = new DataEntryCollection<T> (doc);
            } else {
                collection = new DataEntryCollection<T> (doc.SelectNodes (xpath));
            }
        }

        public string DataUrl {
            get { return data_url; }
        }

#region DataEntryCollection wrapper

        public int Count {
            get { return collection.Count; }
        }

        public T this[int i] {
            get { return collection [i]; }
        }

        public IEnumerator<T> GetEnumerator ()
        {
            return collection.GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return collection.GetEnumerator ();
        }

#endregion

#region Private methods

#endregion

    }
}

