/***************************************************************************
 *  Preferences.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
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
using System.Reflection;
using System.Collections;
using System.Threading;
using Mono.Unix;
using Gtk;
using Glade;

using Nautilus;
using Banshee.Widgets;
using Banshee.MediaEngine;
using Banshee.Base;

namespace Banshee
{
    public class PreferencesWindow
    {
        [Widget] private Window WindowPreferences;
        [Widget] private CheckButton CopyOnImport;
        [Widget] private RadioButton RadioImport;
        [Widget] private RadioButton RadioAppend;
        [Widget] private RadioButton RadioAsk;
        [Widget] private TreeView TreeMimeSynonyms;
        [Widget] private TreeView TreeDecoders;
        [Widget] private Notebook Notebook;
        
        private string oldLibraryLocation;

        private Glade.XML glade;
        
        private PlayerEngine SelectedEngine;
        
        private FileChooserButton libraryLocationChooser;
        
        private PipelineProfileSelector rippingProfile;
        private PipelineProfileSelector ipodProfile;

        public PreferencesWindow()
        {
            glade = new Glade.XML(null, "banshee.glade", "WindowPreferences", null);
            glade.Autoconnect(this);
            
            ((Image)glade["ImageLibraryTab"]).Pixbuf = 
                Gdk.Pixbuf.LoadFromResource("library-icon-32.png");

            ((Image)glade["ImageEncodingTab"]).Pixbuf = 
                Gdk.Pixbuf.LoadFromResource("encoding-icon-32.png");
                
            ((Image)glade["ImageAdvancedTab"]).Pixbuf = 
                Gdk.Pixbuf.LoadFromResource("advanced-icon-32.png");
                    
            IconThemeUtils.SetWindowIcon(WindowPreferences);
                    
            libraryLocationChooser = new FileChooserButton(
                Catalog.GetString("Select Library Location"), 
                FileChooserAction.SelectFolder);
            libraryLocationChooser.SelectionChanged += OnLibraryLocationChooserCurrentFolderChanged;
            (glade["LibraryLocationChooserContainer"] as Container).Add(libraryLocationChooser);
            libraryLocationChooser.Show();
          
            LoadPreferences();
            LoadPlayerEngines();

            WindowPreferences.Show();
            
            rippingProfile = new PipelineProfileSelector();
            (glade["RippingProfileContainer"] as HBox).PackStart(rippingProfile, true, true, 0);
                
            rippingProfile.ProfileKey = GetStringPref(GConfKeys.RippingProfile, "default");
            rippingProfile.Bitrate = GetIntPref(GConfKeys.RippingBitrate, -1);
                
            rippingProfile.Show();
            
            ipodProfile = new PipelineProfileSelector("mp3,aac,mp4,m4a,m4p");
            (glade["IpodProfileContainer"] as HBox).PackStart(ipodProfile, true, true, 0);
                
            ipodProfile.ProfileKey = GetStringPref(GConfKeys.IpodProfile, "default");
            ipodProfile.Bitrate = GetIntPref(GConfKeys.IpodBitrate, -1);
                
            ipodProfile.Show();
        }
        
        private bool GetBoolPref(string key, bool def)
        {
            try {
                return (bool)Globals.Configuration.Get(key);
            } catch(Exception) {
                return def;
            }
        }
        
        private int GetIntPref(string key, int def)
        {
            try {
                return (int)Globals.Configuration.Get(key);
            } catch(Exception) {
                return def;
            }
        }
        
        private string GetStringPref(string key, string def)
        {
            try {
                return (string)Globals.Configuration.Get(key);
            } catch(Exception) {
                return def;
            }
        }
        
        private void OnButtonCancelClicked(object o, EventArgs args)
        {
            WindowPreferences.Destroy();
        }
        
        private void OnButtonOkClicked(object o, EventArgs args)
        {
            SavePreferences();
            WindowPreferences.Destroy();
        }
        
        private void OnLibraryLocationChooserCurrentFolderChanged(object o, EventArgs args)
        {
        }
        
        private void OnButtonLibraryResetClicked(object o, EventArgs args)
        {
            libraryLocationChooser.SetFilename(Paths.DefaultLibraryPath);
        }
        
        private void LoadPreferences()
        {
            oldLibraryLocation = Paths.DefaultLibraryPath;
            
            try {
                oldLibraryLocation = (string)Globals.Configuration.Get(
                        GConfKeys.LibraryLocation);
            } catch(Exception) { }
            
            libraryLocationChooser.SetFilename(oldLibraryLocation);
            
            try {
                CopyOnImport.Active = (bool)Globals.Configuration.Get(
                    GConfKeys.CopyOnImport);
            } catch(Exception) {
                CopyOnImport.Active = false;
            }    
        }
        
        private void SavePreferences()
        {
            //string newLibraryLocation = LibraryLocationEntry.Buffer.Text;
            string newLibraryLocation = libraryLocationChooser.Filename;
        
            if(!oldLibraryLocation.Trim().Equals(newLibraryLocation.Trim())) {
                Globals.Configuration.Set(GConfKeys.LibraryLocation,
                    newLibraryLocation);
                // TODO: Move Library Directory?
            }
            
            Globals.Configuration.Set(GConfKeys.CopyOnImport,
                CopyOnImport.Active);
                
              
            if(rippingProfile != null) {
              try { 
              Globals.Configuration.Set(GConfKeys.RippingProfile,
                rippingProfile.ProfileKey);
                
              Globals.Configuration.Set(GConfKeys.RippingBitrate,
                rippingProfile.Bitrate);
              } catch(Exception) {}
            }
            
            if(ipodProfile != null) {
               try {
                Globals.Configuration.Set(GConfKeys.IpodProfile,
                ipodProfile.ProfileKey);
                
              Globals.Configuration.Set(GConfKeys.IpodBitrate,
                ipodProfile.Bitrate);
                } catch(Exception) {}
            }
            
            SaveEngineSettings();
        }
 
        private void LoadPlayerEngines()
        {
            ListStore store = new ListStore(typeof(string), typeof(string), typeof(PlayerEngine));
            
            ComboBox enginesCombo = new ComboBox();
            enginesCombo.Changed += OnEngineChanged;
            CellRendererText rendererName = new CellRendererText();
            enginesCombo.Model = store;
            enginesCombo.PackStart(rendererName, true);
            enginesCombo.SetAttributes(rendererName, "text", 0);
            
            (glade["EngineComboContainer"] as Box).PackStart(enginesCombo, true, true, 0);
            glade["EngineComboContainer"].ShowAll();
            
            TreeIter activeIter = TreeIter.Zero;
            
            foreach(PlayerEngine engine in PlayerEngineCore.Engines) {
                TreeIter iter = store.AppendValues(engine.Name, engine.Id, engine);
                if(PlayerEngineCore.ActiveEngine.Equals(engine)) {
                    activeIter = iter;
                }
            }
            
            if(!activeIter.Equals(TreeIter.Zero)) {
                enginesCombo.SetActiveIter(activeIter); 
            }
        }
        
        private void OnEngineChanged(object o, EventArgs args)
        {
            ComboBox box = o as ComboBox;
            ListStore store = box.Model as ListStore;
            TreeIter iter;
            
            if(!box.GetActiveIter(out iter)) {
                return;
            }
            
            PlayerEngine engine = store.GetValue(iter, 2) as PlayerEngine;
            SelectedEngine = engine;
        }
        
        private void SaveEngineSettings()
        {
            PlayerEngineCore.DefaultEngine = SelectedEngine;
            PlayerEngineCore.ActiveEngine = SelectedEngine;
        }
    }
}
