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
using Banshee.Gui.Widgets;
using Banshee.Sources.Gui;
using Banshee.Collection;

using Banshee.Gui;
using Banshee.ServiceStack;

namespace Nereid
{

    public class ViewContainer : VBox
    {
        private SearchEntry search_entry;
        private HBox header;
        private Label title_label;
        private Label search_label;
        private VBox footer;
        
        private ISourceContents content;
        
        public ViewContainer ()
        {
            BuildHeader ();           
            
            Spacing = 5;
            SearchSensitive = false;
        }
        
        private void BuildHeader ()
        {
            header = new HBox ();
            footer = new VBox ();
            
            EventBox title_box = new EventBox ();
            title_label = new Label ();
            title_label.Xalign = 0.0f;
            title_label.Ellipsize = Pango.EllipsizeMode.End;

            title_box.Add (title_label);

            // Show the source context menu when the title is right clicked
            title_box.PopupMenu += delegate {
                ServiceManager.Get<InterfaceActionService> ().SourceActions ["SourceContextMenuAction"].Activate ();
            };

            title_box.ButtonPressEvent += delegate (object o, ButtonPressEventArgs press) {
                if (press.Event.Button == 3) {
                    ServiceManager.Get<InterfaceActionService> ().SourceActions ["SourceContextMenuAction"].Activate ();
                }
            };
            
            BuildSearchEntry ();
            
            search_label = new Label (Catalog.GetString ("_Search:"));
            search_label.MnemonicWidget = search_entry.InnerEntry;
            
            header.PackStart (title_box, true, true, 0);
            header.PackStart (search_label, false, false, 5);
            header.PackStart (search_entry, false, false, 0);
            
            InterfaceActionService uia = ServiceManager.Get<InterfaceActionService> ();
            if (uia != null) {
                Gtk.Action action = uia.GlobalActions["WikiSearchHelpAction"];
                if (action != null) {
                    //MenuItem item = new SeparatorMenuItem ();
                    //item.Show ();
                    //search_entry.Menu.Append (item);
                    
                    MenuItem item = new ImageMenuItem (Stock.Help, null);
                    item.Activated += delegate { action.Activate (); };
                    item.Show ();
                    search_entry.Menu.Append (item);
                }
            }
            
            header.ShowAll ();
            search_entry.Show ();
            
            PackStart (header, false, false, 0);
            PackEnd (footer, false, false, 0);
            PackEnd (new ConnectedMessageBar (), false, true, 0);
        }
        
        private void BuildSearchEntry ()
        {
            search_entry = new SearchEntry ();
            search_entry.SetSizeRequest (200, -1);
            
            /*search_entry.AddFilterOption ((int)TrackFilterType.None, Catalog.GetString ("All Columns"));
            search_entry.AddFilterSeparator ();
            search_entry.AddFilterOption ((int)TrackFilterType.SongName, Catalog.GetString ("Song Name"));
            search_entry.AddFilterOption ((int)TrackFilterType.ArtistName, Catalog.GetString ("Artist Name"));
            search_entry.AddFilterOption ((int)TrackFilterType.AlbumTitle, Catalog.GetString ("Album Title"));
            search_entry.AddFilterOption ((int)TrackFilterType.Genre, Catalog.GetString ("Genre"));
            search_entry.AddFilterOption ((int)TrackFilterType.Year, Catalog.GetString ("Year"));  */
            
            search_entry.FilterChanged += OnSearchEntryFilterChanged;
            search_entry.ActivateFilter ((int)TrackFilterType.None);
            
            OnSearchEntryFilterChanged (search_entry, EventArgs.Empty);
        }
        
        private void OnSearchEntryFilterChanged (object o, EventArgs args)
        {
            search_entry.EmptyMessage = String.Format (Catalog.GetString ("Filter on {0}"),
                search_entry.GetLabelForFilterID (search_entry.ActiveFilterID));
        }
        
        public void SetFooter (Widget contents)
        {
            if (contents != null) {
                footer.PackStart (contents, false, false, 0);
                contents.Show ();
                footer.Show ();
            }
        }
        
        public void ClearFooter ()
        {
            foreach (Widget child in footer.Children) {
                footer.Remove (child);
            }
            
            footer.Hide ();
        }
        
        public HBox Header {
            get { return header; }
        }
        
        public SearchEntry SearchEntry {
            get { return search_entry; }
        }
        
        public ISourceContents Content {
            get { return content; }
            set {
                if (content == value) {
                    return;
                }
                
                if (content != null && content.Widget != null) {
                    content.Widget.Hide ();
                    Remove (content.Widget);
                }
                
                content = value;
                
                if (content != null && content.Widget != null) {
                    PackStart (content.Widget, true, true, 0);
                    content.Widget.Show ();
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
                search_entry.Visible = value;
                search_label.Visible = value;
            }
        }
    }
}
