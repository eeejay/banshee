//
// IndexerClient.cs
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
using System.Threading;

using NDesk.DBus;
using org.freedesktop.DBus;

using Banshee.Collection.Indexer;

namespace Banshee.Collection.Indexer.RemoteHelper
{
    public abstract class IndexerClient
    {
        private const string application_bus_name = "org.bansheeproject.Banshee";
        private const string indexer_bus_name = "org.bansheeproject.CollectionIndexer";
        
        private const string service_interface = "org.bansheeproject.CollectionIndexer.Service";
        private static ObjectPath service_path = new ObjectPath ("/org/bansheeproject/Banshee/CollectionIndexerService");
    
        private IBus session_bus;
        private bool listening;
        private ICollectionIndexerService service;
        private bool cleanup_and_shutdown;
        private ManualResetEvent indexer_reset_event = new ManualResetEvent (true);
        
        public void Start ()
        {
            session_bus = Bus.Session.GetObject<IBus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
            session_bus.NameOwnerChanged += OnBusNameOwnerChanged;
            
            if (Bus.Session.NameHasOwner (indexer_bus_name)) {
                ConnectToIndexerService ();
            } else {
                Bus.Session.StartServiceByName (indexer_bus_name);
            }
        }

        private void OnBusNameOwnerChanged (string name, string oldOwner, string newOwner)
        {
            if (name == indexer_bus_name) {
                if (String.IsNullOrEmpty (newOwner)) {
                    // Do not disconnect since we're already disconnected
                    ResetInternalState ();
                } else {
                    ConnectToIndexerService ();
                }
            }
        }
        
        private void Index ()
        {
            if (HasCollectionChanged) {
                ThreadPool.QueueUserWorkItem (delegate {
                    try {
                        UpdateIndex ();
                    } catch (Exception e) {
                        Console.Error.WriteLine (e);
                    }
                    
                    if (!ApplicationAvailable || !listening) {
                        DisconnectFromIndexerService ();
                    }
                });
            }
        }
        
        private void ConnectToIndexerService ()
        {
            DisconnectFromIndexerService ();
            ResolveIndexerService ();
            
            service.CleanupAndShutdown += OnCleanupAndShutdown;
            
            if (ApplicationAvailable) {
                listening = true;
                service.CollectionChanged += OnCollectionChanged;
            }
            
            Index ();
        }
        
        private void DisconnectFromIndexerService ()
        {
            if (service == null) {
                return;
            }
            
            if (listening) {
                try {
                    listening = false;
                    service.CollectionChanged -= OnCollectionChanged;
                } catch {
                }
            }
            
            try {
                service.Shutdown ();
                service.CleanupAndShutdown -= OnCleanupAndShutdown;
            } catch {
            }
            
            ResetInternalState ();
        }
        
        private void ResetInternalState ()
        {
            service = null;
            listening = false;
            cleanup_and_shutdown = false;
            ResetState ();
        }
        
        private void ResolveIndexerService ()
        {
            int attempts = 0;
            
            while (attempts++ < 4) {
                try {
                    service = Bus.Session.GetObject<ICollectionIndexerService> (indexer_bus_name, service_path);
                    service.Hello ();
                    return;
                } catch { 
                    service = null;
                    System.Threading.Thread.Sleep (2000);
                }
            }
        }
        
        private void OnCollectionChanged ()
        {
            Index ();
        }
        
        private void OnCleanupAndShutdown ()
        {
            cleanup_and_shutdown = true;
        }
        
        protected abstract bool HasCollectionChanged { get; }
        
        protected abstract void UpdateIndex ();
        
        protected abstract void ResetState ();
        
        protected bool CleanupAndShutdown {
            get { return cleanup_and_shutdown; }
        }
        
        protected ICollectionIndexerService Service {
            get { return service; }
        }
        
        protected bool ApplicationAvailable {
            get { return Bus.Session.NameHasOwner (application_bus_name); }
        }
    }
}
