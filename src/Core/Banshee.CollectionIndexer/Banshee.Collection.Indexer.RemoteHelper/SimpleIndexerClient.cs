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
    public abstract class SimpleIndexerClient
    {
        private _SimpleIndexerClient client;
        private List<string> export_fields;

        public SimpleIndexerClient ()
        {
            client = new _SimpleIndexerClient (this);
        }

        public void Start ()
        {
            client.Start ();
            OnStarted ();
        }

        public string [] GetAvailableExportFields ()
        {
            return client.GetAvailableExportFields ();
        }

        protected bool IndexWhenCollectionChanged {
            set { client.IndexWhenCollectionChanged = value; }
        }

        public void AddExportField (params string [] fields)
        {
            if (export_fields == null) {
                export_fields = new List<string> ();
            }

            foreach (string field in fields) {
                if (!export_fields.Contains (field)) {
                    export_fields.Add (field);
                }
            }
        }

        public bool RemoveExportField (string field)
        {
            if (export_fields != null) {
                return export_fields.Remove (field);
            }

            return false;
        }

        public IEnumerable<string> ExportFields {
            get {
                if (export_fields == null) {
                    yield break;
                }

                foreach (string field in export_fields) {
                    yield return field;
                }
            }
        }

        protected virtual void OnStarted ()
        {
        }

        protected virtual void OnBeginUpdateIndex ()
        {
        }

        protected virtual void OnEndUpdateIndex ()
        {
        }

        protected abstract void IndexResult (IDictionary<string, object> result);
        protected abstract void OnShutdownWhileIndexing ();
        protected abstract int CollectionCount { get; }
        protected abstract DateTime CollectionLastModified { get; }

        private class _SimpleIndexerClient : IndexerClient
        {
            private object shutdown_mutex = new object ();
            private bool indexer_running;
            private bool shutdown_requested;

            private SimpleIndexerClient parent;

            public _SimpleIndexerClient (SimpleIndexerClient parent)
            {
                this.parent = parent;
            }

            public string [] GetAvailableExportFields ()
            {
                return Service.GetAvailableExportFields ();
            }

            protected override void ResetState ()
            {
                lock (shutdown_mutex) {
                    if (indexer_running) {
                        shutdown_requested = true;
                    }
                }
            }

            protected override void UpdateIndex (ICollectionIndexer indexer)
            {
                lock (shutdown_mutex) {
                    indexer_running = true;
                    shutdown_requested = false;
                }

                bool shutdown_while_indexing = false;

                if (parent.export_fields != null && parent.export_fields.Count > 0) {
                    indexer.SetExportFields (parent.export_fields.ToArray ());
                }

                parent.OnBeginUpdateIndex ();

                for (int i = 0, models = indexer.GetModelCounts (); i < models; i++) {
                    for (int j = 0, items = indexer.GetModelResultsCount (i); j < items; j++) {
                        if (Shutdown) {
                            shutdown_while_indexing = true;
                            break;
                        }

                        parent.IndexResult (indexer.GetResult (i, j));
                    }

                    if (shutdown_while_indexing) {
                        break;
                    }
                }

                lock (shutdown_mutex) {
                    indexer_running = false;
                    shutdown_requested = false;
                }

                if (shutdown_while_indexing) {
                    parent.OnShutdownWhileIndexing ();
                }

                parent.OnEndUpdateIndex ();
            }

            protected override bool HasCollectionChanged {
                get { return Service.HasCollectionCountChanged (parent.CollectionCount) ||
                    Service.HasCollectionLastModifiedChanged (Hyena.DateTimeUtil.ToTimeT (parent.CollectionLastModified)); }
            }

            private bool Shutdown {
                get { lock (shutdown_mutex) { return shutdown_requested || CleanupAndShutdown; } }
            }
        }
    }
}
