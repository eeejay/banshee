//
// Configurationclient.cs
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
using Mono.Addins;

using Hyena;
using Banshee.Base;

namespace Banshee.Configuration
{
    public static class ConfigurationClient
    {
        private static IConfigurationClient client;

        private static void Initialize ()
        {
            lock (typeof (ConfigurationClient)) {
                if (client != null) {
                    return;
                }
                
                if (AddinManager.IsInitialized) {
                    foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (
                        "/Banshee/Platform/ConfigurationClient")) {
                        try {
                            client = (IConfigurationClient)node.CreateInstance (typeof (IConfigurationClient));
                            if (client != null) {
                                break;
                            }
                        } catch (Exception e) {
                            Log.Warning ("Configuration client extension failed to load", e.Message);
                        }
                    }
                    
                    if (client == null) {
                        client = new XmlConfigurationClient ();
                    }
                } else {
                    client = new MemoryConfigurationClient ();
                }
                
                Log.DebugFormat ("Configuration client extension loaded ({0})", client.GetType ().FullName);
            }
        }

        public static IConfigurationClient Client {
            get {
                if (client == null) {
                    Initialize ();
                }
                return client;
            }
        }
        
        public static T Get<T> (SchemaEntry<T> entry)
        {
            return Client.Get<T> (entry);
        }
        
        public static T Get<T> (SchemaEntry<T> entry, T fallback)
        {
            return Client.Get<T> (entry, fallback);
        }
        
        public static T Get<T> (string key, T fallback)
        {
            return Client.Get<T> (key, fallback);
        }
        
        public static T Get<T> (string @namespace, string key, T fallback)
        {
            return Client.Get<T> (@namespace, key, fallback);
        }
        
        public static void Set<T> (SchemaEntry<T> entry, T value)
        {
            Client.Set<T> (entry, value);
        }
        
        public static void Set<T> (string key, T value)
        {
            Client.Set<T> (key, value);
        }
        
        public static void Set<T> (string @namespace, string key, T value)
        {
            Client.Set<T> (@namespace, key, value);
        }
    }
}
