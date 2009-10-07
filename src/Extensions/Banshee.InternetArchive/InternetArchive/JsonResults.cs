//
// JsonResults.cs
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
    public class JsonResults : Results
    {
        JsonObject response_header;
        JsonObject response_header_params;

        JsonObject response;
        JsonArray  response_docs;

        public JsonResults (string resultsString)
        {
            var json = new Deserializer (resultsString).Deserialize () as JsonObject;

            response_header = (JsonObject) json["responseHeader"];
            response_header_params = (JsonObject) response_header["params"];

            response = (JsonObject) json["response"];
            response_docs = (JsonArray) response["docs"];

            TotalResults = (int) (double) response["numFound"];
            Offset = (int) (double) response["start"];
            Count  = Int32.Parse (response_header_params["rows"] as string);
        }

        public override IEnumerable<Item> Items {
            get {
                foreach (object obj in response_docs) {
                    yield return new JsonItem ((JsonObject) obj);
                }
            }
        }
    }
}
