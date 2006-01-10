/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DaapCore.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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
using Mono.Unix;
using DAAP;

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Plugins.Daap
{
    internal static class DaapCore
    {
        private static ServiceLocator locator;
        private static DaapProxyWebServer proxy_server;
        private static Hashtable source_map;
        
        internal static void Initialize()
        {
            source_map = new Hashtable();
            locator = new ServiceLocator();
            locator.Found += OnServiceFound;
            locator.Removed += OnServiceRemoved;
            locator.ShowLocalServices = true;
            locator.Start();
            
            proxy_server = new DaapProxyWebServer(8089);
            proxy_server.Start();
        }
        
        internal static void Dispose()
        {
            if(locator != null) {
                locator.Stop();
                locator.Found -= OnServiceFound;
                locator.Removed -= OnServiceRemoved;
                locator = null;
            }
            
            if(proxy_server != null) {
                proxy_server.Stop();
            }
            
            if(source_map != null) {
                foreach(DaapSource source in source_map.Values) {
                    SourceManager.RemoveSource(source);
                    source.Dispose();
                }
                
                source_map.Clear();
                source_map = null;
            }
        }
        
        private static void OnServiceFound(object o, ServiceArgs args)
        {
            try {
                Source source = new DaapSource(args.Service);
                source_map.Add(args.Service, source);
                SourceManager.AddSource(source);
            } catch(InvalidSourceException) {
            }
        }
        
        private static void OnServiceRemoved(object o, ServiceArgs args)
        {
            Source source = source_map[args.Service] as DaapSource;
            if(source != null) {
                return;
            }
            
            source.Dispose();
            SourceManager.RemoveSource(source);
            source_map.Remove(args.Service);
        }
        
        public static DaapProxyWebServer ProxyServer {
            get {
                return proxy_server;
            }
        }
    }
}
