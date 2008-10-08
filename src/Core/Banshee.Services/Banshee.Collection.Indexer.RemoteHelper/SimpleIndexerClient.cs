//
// SimpleIndexerClient.cs
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

namespace Banshee.Collection.Indexer.RemoteHelper
{
    public abstract class SimpleIndexerClient : IndexerClient
    {
        private object shutdown_mutex = new object ();
        private bool indexer_running;
        private bool shutdown_requested;

        protected override void ResetState ()
        {
            lock (shutdown_mutex) {
                if (indexer_running) {
                    shutdown_requested = true;
                }
            }
        }
        
        protected override void UpdateIndex ()
        {
            lock (shutdown_mutex) {
                indexer_running = true;
                shutdown_requested = false;
            }
            
            bool shutdown_while_indexing = false;;
            ICollectionIndexer indexer = CreateIndexer ();
            indexer.Index ();
            
            for (int i = 0, models = indexer.GetModelCounts (); i < models; i++) {
                for (int j = 0, items = indexer.GetModelResultsCount (i); j < items; j++) {
                    if (Shutdown) {
                        shutdown_while_indexing = true;
                        break;
                    }
                    
                    IndexResult (indexer.GetResult (i, j));
                }
                
                if (shutdown_while_indexing) {
                    break;
                }
            }
            
            indexer.Dispose ();
            
            lock (shutdown_mutex) {
                indexer_running = false;
                shutdown_requested = false;
            }
            
            OnShutdownWhileIndexing ();
        }
        
        protected abstract void OnShutdownWhileIndexing ();
        
        protected abstract void IndexResult (IDictionary<string, object> result);
        
        private bool Shutdown {
            get { lock (shutdown_mutex) { return shutdown_requested || CleanupAndShutdown; } }
        }
    }
}
