//
// GConfConfigurationClient.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;
using GConf;

using Hyena;
using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.GnomeBackend
{
    public class GConfConfigurationClient : IConfigurationClient
    {
        private static string base_key = "/apps/banshee-1/";
        
        private GConf.Client client;
        private Dictionary<string, string> key_table = new Dictionary<string, string> ();
        
        private static bool disable_gconf_checked = false;
        private static bool disable_gconf = false;
        
        private static bool DisableGConf {
            get { 
                if (!disable_gconf_checked) {
                    disable_gconf = ApplicationContext.EnvironmentIsSet ("BANSHEE_DISABLE_GCONF");
                    disable_gconf_checked = true;
                }
                
                return disable_gconf;
            }
        }
        
        private string CreateKeyPart (string part)
        {
            lock (((ICollection)key_table).SyncRoot) {
                if (!key_table.ContainsKey (part)) {
                    key_table.Add (part, StringUtil.CamelCaseToUnderCase (part));
                }
                
                return key_table[part];
            }
        }
        
        private string CreateKey (string @namespace, string key)
        {
            return @namespace == null 
                ? base_key + CreateKeyPart (key)
                : base_key + CreateKeyPart (@namespace.Replace (".", "/")) + "/" + CreateKeyPart (key);
        }
        
        public T Get<T> (SchemaEntry<T> entry)
        {
            return Get<T> (entry.Namespace, entry.Key, entry.DefaultValue);
        }
        
        public T Get<T> (SchemaEntry<T> entry, T fallback)
        {
            return Get<T> (entry.Namespace, entry.Key, fallback);
        }
        
        public T Get<T> (string key, T fallback)
        {
            return Get<T> (null, key, fallback);
        }
        
        public T Get<T> (string @namespace, string key, T fallback)
        {
            if (DisableGConf) {
                return fallback;
            }
            
            if (client == null) {
                client = new GConf.Client ();
            }
            
            try {
                return (T)client.Get (CreateKey (@namespace, key));
            } catch {
                return fallback;
            }
        }
        
        public void Set<T> (SchemaEntry<T> entry, T value)
        {
            Set<T> (entry.Namespace, entry.Key, value);
        }
        
        public void Set<T> (string key, T value)
        {
            Set<T> (null, key, value);
        }
        
        public void Set<T> (string @namespace, string key, T value)
        {
            if (DisableGConf) {
                return;
            }
            
            if (client == null) {
                client = new GConf.Client ();
            }
            
            client.Set (CreateKey (@namespace, key), value);
        }
    }
}
