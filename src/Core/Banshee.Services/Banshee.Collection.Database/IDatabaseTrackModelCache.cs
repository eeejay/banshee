//
// IDatabaseTrackModelCache.cs
//
// Author:
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
using System.Data;
using Banshee.Collection;
using Hyena.Data.Sqlite;

namespace Banshee.Collection.Database
{
    public interface IDatabaseTrackModelCache
    {
        void Clear ();
        void SaveSelection ();
        void UpdateAggregates ();
        void RestoreSelection ();
        long Count { get; }
        void Reload ();
        long IndexOf (Hyena.Data.ICacheableItem item);
        long IndexOf (object item_entry_id);
        TrackInfo GetSingle (string random_fragment, params object [] args);
        TrackInfo GetValue (long index);
        long CacheId { get; }
        event Action<IDataReader> AggregatesUpdated;
    }
}
