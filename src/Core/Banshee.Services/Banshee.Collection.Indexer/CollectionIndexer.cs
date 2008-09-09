//
// CollectionIndexer.cs
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

using Hyena;

using Banshee.Sources;
using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Collection.Indexer
{
    public class CollectionIndexer : ICollectionIndexer, IDisposable
    {
        private static int instance_count = 0;
        
        private CollectionIndexerService service;
        private List<CachedList<DatabaseTrackInfo>> model_caches = new List<CachedList<DatabaseTrackInfo>> ();
        
        private event IndexingFinishedHandler indexing_finished;
        event IndexingFinishedHandler ICollectionIndexer.IndexingFinished {
            add { indexing_finished += value; }
            remove { indexing_finished -= value; }
        }
        
        public event EventHandler IndexingFinished;
        
        internal CollectionIndexer (CollectionIndexerService service)
        {
            this.service = service;
        }
        
        public void Dispose ()
        {
            DisposeModels ();
            
            if (service != null) {
                service.DisposeIndexer (this);
            }
        }
        
        private void DisposeModels ()
        {
            foreach (CachedList<DatabaseTrackInfo> model in model_caches) {
                model.Dispose ();
            }
            
            model_caches.Clear ();
        }
        
        public void Start ()
        {
            lock (this) {
                DisposeModels ();
                
                foreach (Source source in ServiceManager.SourceManager.Sources) {
                    DatabaseSource db_source = source as DatabaseSource;
                    if (db_source != null && db_source.Indexable) {
                        model_caches.Add (CachedList<DatabaseTrackInfo>.CreateFromSourceModel (
                            (DatabaseTrackListModel)db_source.TrackModel));
                    }
                }
            }
            
            OnIndexingFinished ();
        }
        
        public bool SaveToXml (string path)
        {
            lock (this) {
                return false;
            }
        }
        
        public IDictionary<string, object> GetResult (int modelIndex, int itemIndex)
        {
            lock (this) {
                if (modelIndex < 0 || modelIndex >= model_caches.Count) {
                    throw new IndexOutOfRangeException ("modelIndex");
                }
                
                CachedList<DatabaseTrackInfo> model = model_caches[modelIndex];
                
                if (itemIndex < 0 || itemIndex >= model.Count) {
                    throw new IndexOutOfRangeException ("itemIndex");
                }
                
                return model[modelIndex].GenerateExportable ();
            }
        }
        
        public int GetModelCounts ()
        {
            lock (this) {
                return model_caches.Count;
            }
        }
        
        public int GetModelResultsCount (int modelIndex)
        {
            lock (this) {
                if (modelIndex < 0 || modelIndex >= model_caches.Count) {
                    return -1;
                }
                
                return model_caches[modelIndex].Count;
            }
        }
        
        protected virtual void OnIndexingFinished ()
        {
            EventHandler handler = IndexingFinished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        
            IndexingFinishedHandler dbus_handler = indexing_finished;
            if (dbus_handler != null) {
                dbus_handler ();
            }
        }
        
        private string service_name = String.Format ("CollectionIndexer_{0}", instance_count++);
        string IService.ServiceName {
            get { return service_name; }
        }
        
        IDBusExportable IDBusExportable.Parent {
            get { return service; }
        }
    }
}
