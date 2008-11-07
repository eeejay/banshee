//
// PurchasedMusicActions.cs
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
using Gtk;

using Banshee.Dap;
using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Dap.Gui
{
    public class PurchasedMusicActions : BansheeActionGroup
    {
        private IPurchasedMusicSource source;
        
        public PurchasedMusicActions (IPurchasedMusicSource source) : base ("dap-purchased-music")
        {
            this.source = source;
            
            AddImportant (
                new ActionEntry ("PurchasedMusicImportAction", null,
                    Catalog.GetString ("Import Purchased Music"), null,
                    null, OnImportPurchasedMusic)
            );
            
            this["PurchasedMusicImportAction"].IconName = Stock.Save;
            
            Actions.AddActionGroup (this);
        }
        
        public override void Dispose ()
        {
            Actions.RemoveActionGroup (this);
            base.Dispose ();
        }

        private void OnImportPurchasedMusic (object o, EventArgs args)
        {
            source.Import ();
        }
    }
}
