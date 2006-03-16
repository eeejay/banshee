/***************************************************************************
 *  ImportDialog.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using Mono.Unix;
using Gtk;
using Glade;

using Banshee.Base;

namespace Banshee.Gui
{
    public class OpenLocationDialog : GladeDialog
    {
        [Widget] private HBox location_box;
        private ComboBoxEntry address_entry;
        private Button browse_button;
        
        private ArrayList history = new ArrayList();
    
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
            
            ArrayList filtered_history = new ArrayList();
            
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
            
            Globals.Configuration.Set(GConfKeys.OpenLocationHistory, trimmed_history);
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
            try {
                foreach(string uri in (string [])Globals.Configuration.Get(GConfKeys.OpenLocationHistory)) {
                    history.Add(uri);
                    address_entry.AppendText(uri);
                }
            } catch {
            }
        }
        
        public string Address {
            get { return address_entry.Entry.Text; }
        }
    }
}
