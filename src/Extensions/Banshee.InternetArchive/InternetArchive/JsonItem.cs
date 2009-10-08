//
// JsonItem.cs
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
    public static class JsonExtensions
    {
        public static T Get<T> (this JsonObject item, string key)
        {
            object result;
            if (item.TryGetValue (key, out result)) {
                try {
                    return (T)result;
                } catch {
                    Console.WriteLine ("Couldn't cast {0} ({1}) as {2} for key {3}", result, result.GetType (), typeof(T), key);
                }
            }

            return default (T);
        }

        public static string GetJoined (this JsonObject item, string key, string with)
        {
            var ary = item.Get<System.Collections.IEnumerable> (key);
            if (ary != null) {
                return String.Join (with, ary.Cast<object> ().Select (o => o.ToString ()).ToArray ());
            }

            return null;
        }
    }

    public class JsonItem : Item
    {
        JsonObject item;

        public JsonItem (JsonObject item)
        {
            this.item = item;
        }

        public override T Get<T> (Field field)
        {
            return item.Get<T> (field.Id);
        }
    }
}
