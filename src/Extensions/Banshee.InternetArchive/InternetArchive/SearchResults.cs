//
// SearchResults.cs
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
using System.Collections;
using System.Collections.Generic;

using Hyena.Json;

namespace InternetArchive
{
    public sealed class SearchResults : IEnumerable<SearchResult>
    {
        JsonArray results;

        public int TotalResults { get; private set; }
        public int Count  { get; private set; }
        public int Offset { get; private set; }

        public SearchResults (string resultsString)
        {
            var json = new Deserializer (resultsString).Deserialize () as JsonObject;

            var response_header = json.Get<JsonObject> ("responseHeader");
            if (response_header != null) {
                var response_header_params = response_header.Get<JsonObject> ("params");

                Count  = response_header_params.Get<int> ("rows");
            }

            var response = json.Get<JsonObject> ("response");
            if (response != null) {
                results = response.Get<JsonArray> ("docs");

                TotalResults = response.Get<int> ("numFound");
                Offset = response.Get<int> ("start");
            }
        }

        public IEnumerator<SearchResult> GetEnumerator ()
        {
            if (results == null)
                yield break;

            foreach (JsonObject obj in results) {
                yield return new SearchResult (obj);
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
    }
}
