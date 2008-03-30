//
// DaapService.cs
//
// Authors:
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2008 Alexander Hixon
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
using Mono.Unix;
using DAAP;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Sources;
using Banshee.ServiceStack;

namespace Banshee.Daap
{
    public class DaapService : IExtensionService, IDisposable
    {
        private ServiceLocator locator;
        
        private DaapContainerSource container;
        private Dictionary<string, DaapSource> source_map;
        
        void IExtensionService.Initialize ()
        {
            // Add the source, even though its empty, so that the user sees the
            // plugin is enabled, just no child sources yet.
            source_map = new Dictionary<string, DaapSource> ();
            container = new DaapContainerSource ();
            ServiceManager.SourceManager.AddSource (container);
            
            // Now start looking for services.
            // We do this after creating the source because if we do it before
            // there's a race condition where we get a service before the source
            // is added.
            locator = new ServiceLocator ();
            locator.Found += OnServiceFound;
            locator.Removed += OnServiceRemoved;
            locator.ShowLocalServices = true;
            locator.Start ();
        }
        
        public void Dispose ()
        {
            if (locator != null) {
                locator.Stop ();
                locator.Found -= OnServiceFound;
                locator.Removed -= OnServiceRemoved;
                locator = null;
            }
            
            // Dispose any remaining child sources
            foreach (KeyValuePair <string, DaapSource> kv in source_map) {
                kv.Value.Disconnect (true);
                kv.Value.Dispose ();
            }
            
            source_map.Clear ();
        }
        
        private void OnServiceFound (object o, ServiceArgs args)
        {
            DaapSource source = new DaapSource (args.Service);
            source_map.Add (String.Format ("{0}:{1}", args.Service.Address, args.Service.Port), source);
            container.AddChildSource (source);
        }
        
        private void OnServiceRemoved (object o, ServiceArgs args)
        {
            string key = String.Format ("{0}:{1}", args.Service.Address, args.Service.Port);
            DaapSource source = source_map [key];
            
            source.Disconnect (true);
            container.RemoveChildSource (source);
            source_map.Remove (key);
        }
        
        string IService.ServiceName {
            get { return "DaapService"; }
        }
    }
}
