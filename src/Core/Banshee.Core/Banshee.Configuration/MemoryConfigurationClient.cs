//
// MemoryConfigurationClient.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;

namespace Banshee.Configuration
{
    public class MemoryConfigurationClient : IConfigurationClient
    {
        private Dictionary<string, object> config = new Dictionary<string, object> ();

#region Helper Overrides

        public T Get<T> (SchemaEntry<T> entry)
        {
            return Get (entry.Namespace, entry.Key, entry.DefaultValue);
        }

        public T Get<T> (SchemaEntry<T> entry, T fallback)
        {
            return Get (entry.Namespace, entry.Key, fallback);
        }

        public T Get<T> (string key, T fallback)
        {
            return Get (null, key, fallback);
        }

        public void Set<T> (SchemaEntry<T> entry, T value)
        {
            Set (entry.Namespace, entry.Key, value);
        }

        public void Set<T> (string key, T value)
        {
            Set (null, key, value);
        }

#endregion

#region Implementation

        public T Get<T> (string namespce, string key, T fallback)
        {
            lock (this) {
                string fq_key = MakeKey (namespce, key);
                object value;
                
                if (config.TryGetValue (fq_key, out value)) {
                    if (value == null) {
                        return default (T);
                    } else if (value.GetType () == typeof (T)) {
                        return (T)value;
                    }
                }
                
                return fallback;
            }
        }

        public void Set<T> (string namespce, string key, T value)
        {
            lock (this) {
                string fq_key = MakeKey (namespce, key);
                if (config.ContainsKey (fq_key)) {
                    config[fq_key] = value;
                } else {
                    config.Add (fq_key, value);
                }
            }
        }
                
        public static string MakeKey (string namespce, string key)
        {
            return String.Format ("{0}{1}{2}", namespce, String.IsNullOrEmpty (namespce) ? String.Empty : ".", key);
        }
        
#endregion

    }
}
