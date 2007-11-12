// 
// OpenLocationDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using Mono.Unix;
using Gtk;
using Glade;

using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.Gui.Dialogs
{
    public class OpenLocationDialog : GladeDialog
    {
        [Widget] private HBox location_box;
        private ComboBoxEntry address_entry;
        private Button browse_button;
        
        private List<string> history = new List<string>();
    
        public OpenLocationDialog() : base("OpenLocationDialog")
        {
            address_entry = ComboBoxEntry.NewText();
            address_entry.Show();
            address_entry.Entry.Activated += OnEntryActivated;
            
            browse_button = new Button(Catalog.GetString("Browse..."));
            browse_button.Clicked += OnBrowseClicked;
            browse_button.Show();
            
            location_box.PackStart(address_entry, true, true, 0);
            location_box.PackStart(browse_button, false, false, 0);
            
            Dialog.Response += OnResponse;
            LoadHistory();
            
            address_entry.Entry.HasFocus = true;
        }
        
        private void OnEntryActivated(object o, EventArgs args)
        {
            Dialog.Respond(ResponseType.Ok);
        }
        
        private void OnResponse(object o, ResponseArgs args)
        {
            if(args.ResponseId != ResponseType.Ok) {
                return;
            }
            
            List<string> filtered_history = new List<string>();
            
            history.Insert(0, Address);
            foreach(string uri in history) {
                if(!filtered_history.Contains(uri)) {
                    filtered_history.Add(uri);
                }
            }
            
            string [] trimmed_history = new string[Math.Min(15, filtered_history.Count)];
            for(int i = 0; i < trimmed_history.Length; i++) {
                trimmed_history[i] = filtered_history[i] as string;
            }
            
            OpenLocationHistorySchema.Set(trimmed_history);
        }
        
        private void OnBrowseClicked(object o, EventArgs args)
        {
            FileChooserDialog chooser = new FileChooserDialog(
                Catalog.GetString("Open Location"),
                null,
                FileChooserAction.Open
            );
            
            chooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;
            chooser.LocalOnly = false;
            
            if(chooser.Run() == (int)ResponseType.Ok) {
                address_entry.Entry.Text = chooser.Uri;
            }
            
            chooser.Destroy();
        }
        
        private void LoadHistory()
        {
            string [] history_array = OpenLocationHistorySchema.Get();
            if(history_array == null || history_array.Length == 0) {
                return;
            }
            
            foreach(string uri in history_array) {
                history.Add(uri);
                address_entry.AppendText(uri);
            }
        }
        
        public string Address {
            get { return address_entry.Entry.Text; }
        }
        
        public static readonly SchemaEntry<string []> OpenLocationHistorySchema = new SchemaEntry<string []>(
            "player_window", "open_location_history",
            new string [] { String.Empty },
            "URI List",
            "List of URIs in the history drop-down for the open location dialog"
        );
    }
}
