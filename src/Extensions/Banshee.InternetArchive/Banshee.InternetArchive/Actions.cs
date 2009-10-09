//
// Actions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;

using Mono.Unix;
using Gtk;

using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class Actions : Banshee.Gui.BansheeActionGroup
    {
        public Actions (Source source) : base ("InternetArchive")
        {
            Add (
                new ActionEntry ("ViewItemDetails", null, Catalog.GetString ("View Item Details"), null, null, (o, a) => {
                    var item = source.TrackModel.FocusedItem;
                    if (item != null && item.Uri != null) {
                        string [] bits = item.Uri.AbsoluteUri.Split ('/');
                        string id = bits[bits.Length - 1];
                        var src = new ItemSource (item.TrackTitle, id);
                        source.AddChildSource (src);
                        Banshee.ServiceStack.ServiceManager.SourceManager.SetActiveSource (src);
                    }
                }),
                new ActionEntry ("OpenItemWebsite", Stock.JumpTo, Catalog.GetString ("Open Webpage"), null, null, (o, a) => {
                    var item = source.TrackModel.FocusedItem;
                    if (item != null && item.Uri != null) {
                        Banshee.Web.Browser.Open (item.Uri.AbsoluteUri);
                    }
                })
            );

            AddImportant (
                new ActionEntry ("VisitInternetArchive", Stock.JumpTo, Catalog.GetString ("Visit Archive.org"), null, null, (o, a) => {
                    Banshee.Web.Browser.Open ("http://archive.org");
                })
            );

            Register ();
        }
    }
}
