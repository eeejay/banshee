//
// CacheableModelAdapter.cs
//
// Author:
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
using System.Collections.Generic;

namespace Hyena.Data
{
    public abstract class CacheableModelAdapter<T>
    {
        private ICacheableModel model;
        private Dictionary<int, T> cache = new Dictionary<int, T> ();

        public CacheableModelAdapter (ICacheableModel model)
        {
            this.model = model;
        }

        protected virtual Dictionary<int, T> Cache {
            get { return cache; }
        }

        public virtual T GetValue (int index)
        {
            if (Cache.ContainsKey (index))
                return Cache[index];
            
            FetchSet (index, model.FetchCount);
            
            if (Cache.ContainsKey (index))
                return Cache[index];
            
            return default (T);
        }
        
        protected virtual void Add(int key, T value)
        {
            Cache.Add(key, value);
        }

        // Responsible for fetching a set of items and placing them in the cache
        protected abstract void FetchSet (int offset, int limit);

        // Reset the cache and return the total # of items in the model
        public abstract int Reload ();

        public virtual void Clear ()
        {
            InvalidateManagedCache ();
        }

        protected void InvalidateManagedCache ()
        {
            Cache.Clear ();
        }
    }
}
