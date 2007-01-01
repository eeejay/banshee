/***************************************************************************
 *  GConfConfigurationClient.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections;
using System.Collections.Generic;
using GConf;

using Banshee.Base;

namespace Banshee.Configuration
{
    public class GConfConfigurationClient : IConfigurationClient
    {
        private static string base_key = "/apps/banshee/";
        
        private GConf.Client client;
        private Dictionary<string, string> key_table = new Dictionary<string, string>();
        
        public static string BaseKey {
            get { return base_key; }
        }
        
        private string CreateKeyPart(string part)
        {
            lock(((ICollection)key_table).SyncRoot) {
                if(!key_table.ContainsKey(part)) {
                    key_table.Add(part, StringUtil.CamelCaseToUnderCase(part));
                }
                
                return key_table[part];
            }
        }
        
        public string CreateKey(string namespce, string key)
        {
            return namespce == null 
                ? base_key + CreateKeyPart(key)
                : base_key + CreateKeyPart(namespce.Replace(".", "/")) + "/" + CreateKeyPart(key);
        }
        
        public T Get<T>(SchemaEntry<T> entry)
        {
            return Get<T>(entry.Namespace, entry.Key, entry.DefaultValue);
        }
        
        public T Get<T>(SchemaEntry<T> entry, T fallback)
        {
            return Get<T>(entry.Namespace, entry.Key, fallback);
        }
        
        public T Get<T>(string key, T fallback)
        {
            return Get<T>(null, key, fallback);
        }
        
        public T Get<T>(string namespce, string key, T fallback)
        {
            if(client == null) {
                client = new GConf.Client();
            }
            
            try {
                return (T)client.Get(CreateKey(namespce, key));
            } catch(GConf.NoSuchKeyException) {
                return fallback;
            }
        }
        
        public void Set<T>(SchemaEntry<T> entry, T value)
        {
            Set<T>(entry.Namespace, entry.Key, value);
        }
        
        public void Set<T>(string key, T value)
        {
            Set<T>(null, key, value);
        }
        
        public void Set<T>(string namespce, string key, T value)
        {
            if(client == null) {
                client = new GConf.Client();
            }
            
            client.Set(CreateKey(namespce, key), value);
        }
    }
}
