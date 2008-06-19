//
// DataEntryCollection.cs
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
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace Lastfm.Data
{
    public class DataEntryCollection<T> : IEnumerable<T> where T : DataEntry
    {
        private XmlNodeList nodes;
        private Dictionary<int, T> collection = new Dictionary<int, T> ();
        private int count;

        public DataEntryCollection (XmlDocument doc)
        {
            XmlNode node = doc.ChildNodes [doc.ChildNodes.Count - 1];
            nodes = (node.Name == "rss") ? node.SelectNodes ("channel/items") : node.ChildNodes;
            count = nodes.Count;
        }

        public DataEntryCollection (XmlNodeList nodes)
        {
            this.nodes = nodes;
            count = nodes == null ? 0 : nodes.Count;
        }

        public int Count {
            get { return count; }
        }

        public T this[int i] {
            get {
                if (!collection.ContainsKey (i)) {
                    T t = (T) Activator.CreateInstance (typeof(T));
                    t.Root = nodes.Item (i) as XmlElement;
                    collection[i] = t;
                }
                return collection[i];
            }
        }

        public IEnumerator<T> GetEnumerator ()
        {
            for (int i = 0; i < Count; i++) {
                yield return this [i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
    }
}

