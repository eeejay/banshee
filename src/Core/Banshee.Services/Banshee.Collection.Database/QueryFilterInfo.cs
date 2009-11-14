//
// QueryFilterInfo.cs
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
using System.Reflection;

using Hyena.Query;
using Hyena.Data.Sqlite;

using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{
    public class QueryFilterInfo<T> : CacheableItem
    {
        public static SqliteModelProvider<QueryFilterInfo<T>> CreateProvider (string filter_column, QueryField field)
        {
            SqliteModelProvider<QueryFilterInfo<T>> provider = new FilterModelProvider<QueryFilterInfo<T>> (
                ServiceManager.DbConnection,
                "CoreTracks",
                "TrackID", typeof(QueryFilterInfo<T>).GetMember ("DbId")[0] as PropertyInfo,
                filter_column, typeof(QueryFilterInfo<T>).GetMember ("Value")[0] as PropertyInfo
            );

            return provider;
        }

        private long dbid;
        public long DbId {
            get { return dbid; }
            set { dbid = value; }
        }

        private T obj;
        public T Value {
            get { return obj; }
            set { obj = value; }
        }

        private string title;
        public string Title {
            get { return title ?? ToString (); }
            set { title = value; }
        }

        public object ValueObject { get { return Value; } }

        public override string ToString ()
        {
            return Value == null ? String.Empty : Value.ToString ();
        }
    }
}
