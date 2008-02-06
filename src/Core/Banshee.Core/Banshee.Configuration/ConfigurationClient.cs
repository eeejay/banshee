/***************************************************************************
 *  ConfigurationClient.cs
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
namespace Banshee.Configuration
{
    public static class ConfigurationClient
    {
        private static IConfigurationClient client;

        private static IConfigurationClient Client {
            get {
                if(client == null) {
                    client = new XmlConfigurationClient();
                }
                return client;
            }
        }
        
        public static T Get<T>(SchemaEntry<T> entry)
        {
            return Client.Get<T>(entry);
        }
        
        public static T Get<T>(SchemaEntry<T> entry, T fallback)
        {
            return Client.Get<T>(entry, fallback);
        }
        
        public static T Get<T>(string key, T fallback)
        {
            return Client.Get<T>(key, fallback);
        }
        
        public static T Get<T>(string namespce, string key, T fallback)
        {
            return Client.Get<T>(namespce, key, fallback);
        }
        
        public static void Set<T>(SchemaEntry<T> entry, T value)
        {
            Client.Set<T>(entry, value);
        }
        
        public static void Set<T>(string key, T value)
        {
            Client.Set<T>(key, value);
        }
        
        public static void Set<T>(string namespce, string key, T value)
        {
            Client.Set<T>(namespce, key, value);
        }
    }
}