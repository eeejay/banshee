//
// AlbumListModel.cs
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

namespace Banshee.Collection
{
    public class AlbumListModel : ExportableModel, IListModel<AlbumInfo>
    {
        public event EventHandler Cleared;
        public event EventHandler Reloaded;
        
        public AlbumListModel()
        {
        }
        
        public AlbumListModel(IDBusExportable parent) : base(parent)
        {
        }
        
        protected virtual void OnCleared()
        {
            EventHandler handler = Cleared;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnReloaded()
        {
            EventHandler handler = Reloaded;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        public virtual void Clear()
        {
            throw new NotImplementedException();
        }
        
        public virtual void Reload()
        {
            throw new NotImplementedException();
        }
    
        public virtual AlbumInfo GetValue(int index)
        {
            throw new NotImplementedException();
        }
        
        public virtual IEnumerable<ArtistInfo> ArtistInfoFilter {
            set { throw new NotImplementedException(); }
        }

        public virtual int Rows { 
            get { throw new NotImplementedException(); }
        }
    }
}
