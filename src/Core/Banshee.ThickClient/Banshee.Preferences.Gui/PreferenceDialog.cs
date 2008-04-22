//
// PreferencesDialog.cs
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
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Hyena;
using Banshee.ServiceStack;
using Banshee.Preferences;

using Banshee.Gui;
using Banshee.Gui.Dialogs;

namespace Banshee.Preferences.Gui
{
    public class PreferenceDialog : BansheeDialog
    {
        private PreferenceService service;
        
        private Dictionary<string, NotebookPage> pages = new Dictionary<string, NotebookPage> ();
        private Notebook notebook;
        
        public PreferenceDialog () : base (Catalog.GetString ("Preferences"))
        {
            service = ServiceManager.Get<PreferenceService> ();
            
            if (service == null) {
                Log.Error (Catalog.GetString ("Could not show preferences"), 
                Catalog.GetString ("The preferences service could not be found."), true);
                
                throw new ApplicationException ();
            }
            
            DefaultPreferenceWidgets.Load (service);
            service.RequestWidgetAdapters ();
            
            BuildDialog ();
            LoadPages ();
        }
        
        private void BuildDialog ()
        {
            AddDefaultCloseButton ();
            
            if (service.Count > 1) {
                notebook = new Notebook ();
                notebook.Show ();
            
                VBox.PackStart (notebook, true, true, 0);
            }
        }
        
        private void LoadPages ()
        {
            foreach (Page page in service) {
                LoadPage (page);
            }
        }
        
        private void LoadPage (Page page)
        {
            if (pages.ContainsKey (page.Id)) {
                Log.Warning (String.Format ("Preferences notebook already contains a page with the id `{0}'", 
                    page.Id), false);
                return;
            }
            
            NotebookPage page_ui = new NotebookPage (page);
            page_ui.Show ();
            pages.Add (page.Id, page_ui);
            
            if (service.Count == 1) {
                VBox.PackStart (page_ui, false, false, 0);
            } else {
                notebook.AppendPage (page_ui, page_ui.TabWidget);
            }
        }
    }
}
