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
        public Actions (HomeSource source) : base ("InternetArchive")
        {
            Add (
                new ActionEntry ("IaResultPopup", null, null, null, null, (o, a) => {
                    ShowContextMenu ("/IaResultPopup");
                }),
                new ActionEntry ("ViewItemDetails", null, Catalog.GetString ("View Item Details"), null, null, (o, a) => {
                    var item = source.SearchSource.FocusedItem;
                    if (item != null && item.Id != null) {
                        string id = item.Id;
                        var src = new DetailsSource (id, item.Title, item.MediaType);
                        source.AddChildSource (src);
                        Banshee.ServiceStack.ServiceManager.SourceManager.SetActiveSource (src);
                    }
                }),
                new ActionEntry ("OpenItemWebsite", Stock.JumpTo, Catalog.GetString ("Open Webpage"), null, null, (o, a) => {
                    string uri = null;
                    var src = ActiveSource as DetailsSource;
                    if (src != null) {
                        uri = src.Item.Details.WebpageUrl;
                    } else {
                        var item = source.SearchSource.FocusedItem;
                        if (item != null) {
                            uri = item.WebpageUrl;
                        }
                    }

                    if (uri != null) {
                        Banshee.Web.Browser.Open (uri);
                    }
                })
            );

            AddImportant (
                new ActionEntry ("VisitInternetArchive", Stock.JumpTo, Catalog.GetString ("Visit Archive.org"), null, null, (o, a) => {
                    Banshee.Web.Browser.Open ("http://archive.org");
                })
            );

            AddUiFromFile ("GlobalUI.xml");

            Register ();
        }
    }
}
