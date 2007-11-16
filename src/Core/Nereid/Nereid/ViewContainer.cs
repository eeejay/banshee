// 
// ViewContainer.cs
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
using Gtk;
using Mono.Unix;

using Banshee.Widgets;
using Banshee.Collection;

namespace Nereid
{
    public class ViewContainer : VBox
    {
        private SearchEntry search_entry;
        private HBox header;
        private Label title_label;
        private Label search_label;
        
        private Widget content;
        
        public ViewContainer ()
        {
            BuildHeader ();           
            
            Spacing = 5;
            SearchSensitive = false;
        }
        
        private void BuildHeader ()
        {
            header = new HBox ();
            
            title_label = new Label ();
            title_label.Xalign = 0.0f;
            
            BuildSearchEntry ();
            
            search_label = new Label (Catalog.GetString ("_Search:"));
            search_label.MnemonicWidget = search_entry.InnerEntry;
            
            header.PackStart (title_label, true, true, 0);
            header.PackStart (search_label, false, false, 5);
            header.PackStart (search_entry, false, false, 0);
            
            header.ShowAll ();
            search_entry.Show ();
            
            PackStart (header, false, false, 0);
        }
        
        private void BuildSearchEntry ()
        {
            search_entry = new SearchEntry ();
            search_entry.SetSizeRequest (200, -1);
            
            search_entry.AddFilterOption ((int)TrackFilterType.None, Catalog.GetString ("All Columns"));
            search_entry.AddFilterSeparator ();
            search_entry.AddFilterOption ((int)TrackFilterType.SongName, Catalog.GetString ("Song Name"));
            search_entry.AddFilterOption ((int)TrackFilterType.ArtistName, Catalog.GetString ("Artist Name"));
            search_entry.AddFilterOption ((int)TrackFilterType.AlbumTitle, Catalog.GetString ("Album Title"));
            search_entry.AddFilterOption ((int)TrackFilterType.Genre, Catalog.GetString ("Genre"));
            search_entry.AddFilterOption ((int)TrackFilterType.Year, Catalog.GetString ("Year"));  
            
            search_entry.FilterChanged += OnSearchEntryFilterChanged;
            search_entry.ActivateFilter ((int)TrackFilterType.None);
            
            OnSearchEntryFilterChanged (search_entry, EventArgs.Empty);
        }
        
        private void OnSearchEntryFilterChanged (object o, EventArgs args)
        {
            search_entry.EmptyMessage = String.Format (Catalog.GetString ("Filter on {0}"),
                search_entry.GetLabelForFilterID (search_entry.ActiveFilterID));
        }
        
        public HBox Header {
            get { return header; }
        }
        
        public SearchEntry SearchEntry {
            get { return search_entry; }
        }
        
        public Widget Content {
            get { return content; }
            set {
                if (content != null) {
                    content.Hide ();
                    Remove (content);
                }
                
                content = value;
                
                if (content != null) {
                    PackStart (content, true, true, 0);
                    content.Show ();
                }
            }
        }
        
        public string Title {
            set { title_label.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (value)); }
        }
        
        public bool SearchSensitive {
            get { return search_entry.Sensitive; }
            set { 
                search_entry.Sensitive = value;
                search_label.Sensitive = value;
            }
        }
    }
}
