//
// MediaGroupSource.cs
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

using Mono.Unix;

using Hyena.Query;
using Banshee.Query;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.SmartPlaylist;

namespace Banshee.Dap
{
    public abstract class MediaGroupSource : SmartPlaylistSource
    {
        private DapSource parent;
        
        public MediaGroupSource (DapSource parent, string name) : base (name, parent)
        {
            this.parent = parent;
            
            Properties.Set<Gtk.Widget> ("Nereid.SourceContents.FooterWidget", 
                parent.Properties.Get<Gtk.Widget> ("Nereid.SourceContents.FooterWidget"));
            
            Properties.Set<string> ("DeleteTracksFromDriveActionLabel", 
                String.Format (Catalog.GetString ("Delete From {0}"), parent.Name));
        }

        protected override void AfterInitialized ()
        {
            base.AfterInitialized ();
            Reload ();
        }
        
        protected override void OnUpdated ()
        {
            base.OnUpdated ();
            if (parent != null) {
                parent.RaiseUpdated ();
            }
            
            if (AutoHide) {
                bool contains_me = parent.ContainsChildSource (this);
                int count = Count;
                
                if (count == 0 && contains_me) {
                    parent.RemoveChildSource (this);
                } else if (count > 0 && !contains_me) {
                    parent.AddChildSource (this);
                }
            }
        }

        /*public override bool AcceptsInputFromSource (Source source)
        {
            return (source is DatabaseSource) && source.Parent != Parent && source != Parent;
        }*/
        
        private bool auto_hide;
        public virtual bool AutoHide {
            get { return auto_hide; }
            set { auto_hide = value; }
        }
        
        public override string ConditionSql {
            get { return base.ConditionSql; }
            protected set { 
                base.ConditionSql = value;
                Save ();
            }
        }
        
        public override bool CanRename {
            get { return false; }
        }
        
        public override bool CanUnmap {
            get { return false; }
        }
        
        public override bool HasProperties {
            get { return false; }
        }

        public long BytesUsed {
            get { return DatabaseTrackModel.UnfilteredFileSize; }
        }
    }
}
