//
// RemoteClient.cs
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

namespace Beroe
{
    public class RemoteClient : Banshee.Collection.Indexer.RemoteHelper.IndexerClient
    {
        private object shutdown_mutex = new object ();
        private bool indexer_running;
        private bool shutdown_requested;
        
        public RemoteClient ()
        {
            ShowDebugMessages = true;
        }
        
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
            
            int i = 0;
            Console.Write ("Updating Index... ");
            while (i++ < 20 && !Shutdown) {
                Console.Write ("{0} ", i);
                System.Threading.Thread.Sleep (1000);
            }
            Console.WriteLine (": Done");
            
            lock (shutdown_mutex) {
                indexer_running = false;
                shutdown_requested = false;
            }
        }
        
        private bool Shutdown {
            get { lock (shutdown_mutex) { return shutdown_requested || CleanupAndShutdown; } }
        }
        
        protected override bool HasCollectionChanged {
            get { return true; }
        }
    }
}
