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

using NDesk.DBus;

using Banshee.ServiceStack;

namespace Banshee.Collection.Indexer
{
    public class CollectionIndexerService : ICollectionIndexerService
    {
        private List<TrackListModel> models = new List<TrackListModel> ();
        private string [] available_export_fields;
        
        public void AddModel (TrackListModel model)
        {
            models.Add (model);
        }
        
        public IEnumerable<IDictionary<string, object>> CreateIndex ()
        {
            yield break;
        }
        
        public IEnumerable<IDictionary<string, object>> GenerateExportable ()
        {
            foreach (TrackListModel model in models) {
                model.Reload ();
                for (int i = 0, n = model.Count; i < n; i++) {
                    yield return model[i].GenerateExportable ();
                }
            }
        }
        
        public ICollectionIndexer CreateIndexer ()
        {
            return new CollectionIndexer (null);
        }
        
        internal void DisposeIndexer (CollectionIndexer indexer)
        {
            ServiceManager.DBusServiceManager.UnregisterObject (indexer);
        }
        
        ObjectPath ICollectionIndexerService.CreateIndexer ()
        {
            return ServiceManager.DBusServiceManager.RegisterObject (new CollectionIndexer (this));
        }
        
        public string [] GetAvailableExportFields ()
        {
            if (available_export_fields != null) {
                return available_export_fields;
            }
            
            List<string> fields = new List<string> ();
            
            foreach (KeyValuePair<string, System.Reflection.PropertyInfo> field in TrackInfo.GetExportableProperties (
                typeof (Banshee.Collection.Database.DatabaseTrackInfo))) {
                fields.Add (field.Key);
            }
            
            available_export_fields = fields.ToArray ();
            return available_export_fields;
        }
        
        IDBusExportable IDBusExportable.Parent { 
            get { return null; }
        }
        
        string IService.ServiceName {
            get { return "CollectionIndexerService"; }
        }
    }
}
