//
// LastfmDataCollection.cs
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
    public class LastfmDataCollection<T> : LastfmData, IEnumerable<T> where T : DataEntry
    {
        private XmlNode root;
        private Dictionary<int, T> collection = new Dictionary<int, T> ();
        private int count;

        public LastfmDataCollection (string dataUrlFragment) : base (dataUrlFragment)
        {
            Initialize ();
        }

        public LastfmDataCollection (string dataUrlFragment, CacheDuration cacheDuration) : base (dataUrlFragment, cacheDuration)
        {
            Initialize ();
        }

        private void Initialize ()
        {
            if (doc.ChildNodes.Count == 2)
                root = doc.ChildNodes [1];
            else
                root = doc.FirstChild;

            count = root.ChildNodes.Count;
        }

        public int Count {
            get { return count; }
        }

        public T this[int i] {
            get {
                if (!collection.ContainsKey (i)) {
                    T t = (T) Activator.CreateInstance (typeof(T));
                    t.Root = root.ChildNodes.Item (i);
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

