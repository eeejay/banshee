/***************************************************************************
 *  PreferencesDialog.cs
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
using System.IO;

using Gtk;
using Glade;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.Gui.Dialogs
{
    public class PreferencesDialog : GladeDialog
    {
        [Widget] private Button library_reset;
        [Widget] private CheckButton copy_on_import;
        
        private FileChooserButton library_location_chooser;
        private PipelineProfileSelector cd_importing_profile;
        private PipelineProfileSelector dap_conversion_profile;
        
        public PreferencesDialog() : base("PreferencesDialog")
        {
            BuildWindow();
            LoadPreferences();
            ConnectEvents();
        }
        
        private void BuildWindow()
        {
            library_location_chooser = new FileChooserButton(Catalog.GetString("Select library location"),
                FileChooserAction.SelectFolder);
            (Glade["library_location_container"] as Container).Add(library_location_chooser);
            library_location_chooser.Show();
            
            cd_importing_profile = new PipelineProfileSelector();
            (Glade["cd_importing_profile_container"] as Box).PackStart(cd_importing_profile, false, false, 0);
            cd_importing_profile.Show();
            
            dap_conversion_profile = new PipelineProfileSelector("mp3,aac,mp4,m4a,m4p");
            (Glade["dap_conversion_profile_container"] as Box).PackStart(dap_conversion_profile, false, false, 0);
            dap_conversion_profile.Show();
        }
        
        private void LoadPreferences()
        {                   
            string location = (string)ReadPreference(GConfKeys.LibraryLocation, Paths.DefaultLibraryPath);
            if(Directory.Exists(location)) {
                location = Paths.DefaultLibraryPath;
            }
            
            library_location_chooser.SetFilename(location);
            SaveLibraryLocation(location);   

            copy_on_import.Active = (bool)ReadPreference(GConfKeys.CopyOnImport, false);

            cd_importing_profile.ProfileKey = (string)ReadPreference(GConfKeys.RippingProfile, "default");
            cd_importing_profile.Bitrate = (int)ReadPreference(GConfKeys.RippingBitrate, -1);
            
            dap_conversion_profile.ProfileKey = (string)ReadPreference(GConfKeys.IpodProfile, "default");
            dap_conversion_profile.Bitrate = (int)ReadPreference(GConfKeys.IpodBitrate, -1);
        }
        
        private void ConnectEvents()
        {
            library_reset.Clicked += delegate { 
                library_location_chooser.SetFilename(Paths.DefaultLibraryPath);
            };
            
            library_location_chooser.SelectionChanged += delegate {
                SaveLibraryLocation(library_location_chooser.Filename);
            };
            
            copy_on_import.Toggled += delegate {
                Globals.Configuration.Set(GConfKeys.CopyOnImport, copy_on_import.Active);
            };
            
            cd_importing_profile.Changed += delegate {
                if(cd_importing_profile == null) {
                    return;
                }
                
                try {
                    Globals.Configuration.Set(GConfKeys.RippingProfile, cd_importing_profile.ProfileKey);
                    Globals.Configuration.Set(GConfKeys.RippingBitrate, cd_importing_profile.Bitrate);
                } catch {
                }
            };
            
            dap_conversion_profile.Changed += delegate {
                if(dap_conversion_profile == null) {
                    return;
                }
                
                try {
                    Globals.Configuration.Set(GConfKeys.IpodProfile, dap_conversion_profile.ProfileKey);
                    Globals.Configuration.Set(GConfKeys.IpodBitrate, dap_conversion_profile.Bitrate);
                } catch {
                }
            };
        }
        
        private void SaveLibraryLocation(string path)
        {
            Globals.Configuration.Set(GConfKeys.LibraryLocation, path);
        }
        
        private object ReadPreference(string key, object fallback)
        {
            try {
                object o = Globals.Configuration.Get(key);
                if(o.GetType() != fallback.GetType()) {
                    Console.Error.WriteLine("Preference for key '{0}' has invalid type '{1}' (expected '{2}')",
                        key, o.GetType(), fallback.GetType());
                    return fallback;
                }
                
                return o;
            } catch {
                return fallback;
            }
        }
    }
}
