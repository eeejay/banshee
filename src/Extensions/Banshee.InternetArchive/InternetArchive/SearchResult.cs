//
// SearchResult.cs
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

using Hyena.Json;

namespace InternetArchive
{
    public sealed class SearchResult
    {
        JsonObject item;

        internal SearchResult (JsonObject item)
        {
            this.item = item;
        }

        public string Id {
            get { return Get<string> (Field.Identifier); }
        }

        public string WebpageUrl {
            get { return String.Format ("http://www.archive.org/details/{0}", Id); }
        }

        public DateTime DateAdded {
            get { return GetDate (Field.DateAdded.Id); }
        }

        public string Creator {
            get { return GetJoined (Field.Creator, ", ") ?? ""; }
        }

        public string Description {
            get { return Hyena.StringUtil.RemoveHtml (Get<string> (Field.Description)); }
        }

        public string Publisher {
            get { return GetJoined (Field.Publisher, ", ") ?? ""; }
        }

        public string LicenseUrl {
            get { return Get<string> (Field.LicenseUrl) ?? item.GetJoined ("license", null); }
        }

        public int Downloads {
            get { return Get<int> (Field.Downloads); }
        }

        public double AvgRating {
            get { return Get<double> (Field.AvgRating); }
        }

        public int AvgRatingInt {
            get { return (int) Math.Round (AvgRating); }
        }

        public string Title {
            get { return Get<string> (Field.Title); }
        }

        public string Format {
            get { return GetJoined (Field.Format, ", "); }
        }

        public string MediaType {
            get { return Get<string> (Field.MediaType); }
        }

        public int Year {
            get { return Get<int> (Field.Year); }
        }

        public T Get<T> (Field field)
        {
            return item.Get<T> (field.Id);
        }

        public DateTime GetDate (string key)
        {
            DateTime ret;
            return DateTime.TryParse (item.GetJoined (key, null), out ret) ? ret : DateTime.MinValue;
        }

        public string GetJoined (Field field, string with)
        {
            return item.GetJoined (field.Id, with);
        }
    }
}
