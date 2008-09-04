//
// DapActions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using Gtk;

using Banshee.Dap;
using Banshee.Gui;

namespace Banshee.Dap.Gui
{
    public class DapActions : BansheeActionGroup
    {
        private DapSource dap;
        public DapActions (DapSource dap) : base ("dap")
        {
            this.dap = dap;
            AddImportant (
                new ActionEntry ("SyncDapAction", null,
                    Catalog.GetString ("Synchronize"), null,
                    String.Format (Catalog.GetString ("Synchronize {0}"), dap.Name), OnSyncDap)
            );
            
            this["SyncDapAction"].IconName = Stock.Refresh;
            UpdateActions ();
            dap.Sync.Updated += delegate { Banshee.Base.ThreadAssist.ProxyToMain (UpdateActions); };
        }

        private void UpdateActions ()
        {
            UpdateAction ("SyncDapAction", dap.Sync.Enabled && !dap.Sync.AutoSync);
        }

        private void OnSyncDap (object o, EventArgs args)
        {
            Banshee.Base.ThreadAssist.SpawnFromMain (delegate {
                dap.Sync.Sync ();
            });
        }
    }
}
