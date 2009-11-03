//
// SearchDescription.cs
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

using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class SearchDescription
    {
        public string  Name  { get; set; }
        public string  Query { get; set; }
        public IA.Sort Sort  { get; set; }
        public IA.FieldValue MediaType  { get; set; }

        public SearchDescription (string name, string query, IA.Sort sort, IA.FieldValue type)
        {
            Name = name;
            Query = query;
            Sort = sort;
            MediaType = type;
        }

        public void ApplyTo (IA.Search search)
        {
            search.Sorts.Clear ();

            search.Sorts.Add (Sort);

            // And if the above sort value is the same for two items, sort by creator then by title
            search.Sorts.Add (IA.Sort.CreatorAsc);
            search.Sorts.Add (IA.Sort.TitleAsc);

            string query = MediaType != null ? MediaType.ToString () + " AND " : "";

            // Remove medialess 'collection' results
            query += "-mediatype:collection";

            if (!String.IsNullOrEmpty (Query)) {
                query += String.Format (" AND {0}", Query);
            }

            search.Query = query;
        }
    }
}
