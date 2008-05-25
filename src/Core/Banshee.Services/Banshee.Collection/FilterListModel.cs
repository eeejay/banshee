//
// BrowsableListModel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Hyena.Data;
using Banshee.ServiceStack;

// TODO remove this
using Banshee.Collection.Database;

namespace Banshee.Collection
{
    public abstract class FilterListModel<T> : BansheeListModel<T>, IFilterListModel
    {
        // TODO refactor/pull out interface so this isn't track specific
        private readonly DatabaseTrackListModel browsing_model;
        protected DatabaseTrackListModel FilteredModel {
            get { return browsing_model; }
        }

        public FilterListModel (DatabaseTrackListModel trackModel) : base ()
        {
            browsing_model = trackModel;
            
            selection = new SelectAllSelection ();
            selection.SelectAll ();
            
            Selection.Changed += HandleSelectionChanged;
        }
        
        public FilterListModel (IDBusExportable parent) : base (parent)
        {
            selection = new SelectAllSelection ();
            selection.SelectAll ();
        }
        
        public override void Reload ()
        {
            Reload (false);
        }
        
        public abstract void Reload (bool notify);
        
        /*protected virtual void GenerateReloadFragment ()
        {
            ReloadFragment = String.Format (
                ReloadFragmentFormat,
                browsing_model.CachesJoinTableEntries ? browsing_model.JoinFragment : null,
                browsing_model.CacheId,
                browsing_model.CachesJoinTableEntries
                    ? String.Format (
                        "{0}.{1} AND CoreTracks.TrackID = {0}.{2}",
                        browsing_model.JoinTable, browsing_model.JoinPrimaryKey, browsing_model.JoinColumn
                    ) : "CoreTracks.TrackID",
                GetFilterFragment ()
            );
        }*/
        
        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            browsing_model.Reload (this);
        }
        
#region IFilterModel Implementation

        public abstract string GetSqlFilter ();

#endregion
        
        //public abstract void RaiseReloaded ();
    }
}
