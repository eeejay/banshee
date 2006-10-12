/***************************************************************************
 *  ProfileComboBox.cs
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
using Mono.Unix;
using Gtk;

namespace Banshee.AudioProfiles.Gui
{
    public class ProfileComboBox : ComboBox
    {
        private ProfileManager manager;
        private ListStore store;
        
        public event EventHandler Updated;
        
        public ProfileComboBox(ProfileManager manager)
        {
            this.manager = manager;
            
            BuildWidget();
            ReloadProfiles();
        }
        
        private void BuildWidget()
        {
            store = new ListStore(typeof(string), typeof(Profile));
            store.RowInserted += delegate { OnUpdated(); };
            store.RowDeleted += delegate { OnUpdated(); };
            store.RowChanged += delegate { OnUpdated(); };
            Model = store;
            
            CellRendererText text_renderer = new CellRendererText();
            PackStart(text_renderer, true);
            AddAttribute(text_renderer, "text", 0);
        }
        
        public void ReloadProfiles()
        {
            TreeIter active_iter;
            store.Clear();
            
            if(manager.AvailableProfileCount == 0) {
                store.AppendValues(Catalog.GetString("No available profiles"), null);
                Sensitive = false;
            } else {
                Sensitive = true;
            }
            
            foreach(Profile profile in manager.GetAvailableProfiles()) {
                store.AppendValues(String.Format("{0}", profile.Name), profile);
            }
            
            if(store.IterNthChild(out active_iter, 0)) {
                SetActiveIter(active_iter);
            }
        }

        public void SetActiveProfile(Profile profile)
        {
            for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                TreeIter iter;
                if(store.IterNthChild(out iter, i)) {
                    Profile compare_profile = (Profile)store.GetValue(iter, 1);
                    if(profile == compare_profile) {
                        SetActiveIter(iter);
                        return;
                    }
                }
            }
        }
        
        protected virtual void OnUpdated()
        {
            EventHandler handler = Updated;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public Profile ActiveProfile {
            get { 
                TreeIter iter;
                if(GetActiveIter(out iter)) {
                    return store.GetValue(iter, 1) as Profile;
                }
                
                return null;
            }
            
            set { SetActiveProfile(value); }
        }
    }
}
