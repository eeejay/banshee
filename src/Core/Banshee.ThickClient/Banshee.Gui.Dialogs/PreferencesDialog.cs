//
// PreferencesDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.IO;

using Gtk;
using Glade;
using Mono.Unix;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Widgets;
using Banshee.AudioProfiles;
using Banshee.AudioProfiles.Gui;
using Banshee.Configuration.Schema;

namespace Banshee.Gui.Dialogs
{
    public class PreferencesDialog : GladeDialog
    {
        [Widget] private Button library_reset;
        [Widget] private CheckButton copy_on_import;
        [Widget] private CheckButton write_metadata;
        [Widget] private CheckButton error_correction;
        [Widget] private Table organization_table;
        
        private Tooltips tips = new Tooltips();
        private FileChooserButton library_location_chooser;
        private ProfileComboBoxConfigurable cd_importing_profile_box;
        private DictionaryComboBox<string> folder_box;
        private DictionaryComboBox<string> file_box;
        
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
            (Glade["library_location_label"] as Label).MnemonicWidget = library_location_chooser;
            library_location_chooser.Show();
            
            cd_importing_profile_box = new ProfileComboBoxConfigurable(ServiceManager.ProfileManager, "cd-importing", 
                Glade["cd_importing_profile_description"] as Box);
            (Glade["cd_importing_profile_container"] as Box).PackStart(cd_importing_profile_box, false, false, 0);  
            (Glade["cd_importing_profile_label"] as Label).MnemonicWidget = cd_importing_profile_box.Combo;        
            cd_importing_profile_box.Show();
            
            string file_pattern = LibrarySchema.FilePattern.Get(FileNamePattern.DefaultFile);
            string folder_pattern = LibrarySchema.FolderPattern.Get(FileNamePattern.DefaultFolder); 
            bool preference_already_added;
            
            folder_box = new DictionaryComboBox<string>();
            preference_already_added = false;
            foreach(string pattern in FileNamePattern.SuggestedFolders) {
                if (pattern.Equals(folder_pattern)) {
                    preference_already_added = true;
                }
                folder_box.Add(FileNamePattern.CreatePatternDescription(pattern), pattern);
            }
            
            if(!preference_already_added) {
                folder_box.Add(FileNamePattern.CreatePatternDescription(folder_pattern), folder_pattern);
            }
            
            preference_already_added = false;
            file_box = new DictionaryComboBox<string>();
            foreach(string pattern in FileNamePattern.SuggestedFiles) {
                if (pattern.Equals(file_pattern)) {
                    preference_already_added = true;
                }
                file_box.Add(FileNamePattern.CreatePatternDescription(pattern), pattern);
            }
            
            if(!preference_already_added) {
                file_box.Add(FileNamePattern.CreatePatternDescription(file_pattern), file_pattern);
            }
            
            organization_table.Attach(folder_box, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Fill, 0, 0);
            (Glade["folder_label"] as Label).MnemonicWidget = folder_box;
            folder_box.Show();
            
            organization_table.Attach(file_box, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Fill, 0, 0);
            (Glade["file_label"] as Label).MnemonicWidget = file_box;
            file_box.Show();
            
            tips.SetTip(Glade["write_metadata"], Catalog.GetString(
                "Enable this option to save tags and other metadata inside supported audio files"),
                "write-metadata");
                
            tips.SetTip(Glade["error_correction"], Catalog.GetString(
                "Error correction tries to work around problem areas on a disc, such " +
                "as surface scratches, but will slow down importing substantially."),
                "error-correction");
                
            //(Glade["error_correction"] as CheckButton).Sensitive = (Glade["cd_importing_profile_label"] as Label).Sensitive;
        }
        
        private void LoadPreferences()
        {                   
            string location = Paths.LibraryLocation;
            if(!Directory.Exists(location)) {
                location = Paths.DefaultLibraryPath;
            }
            
            library_location_chooser.SetCurrentFolder(location);
            Paths.LibraryLocation = location;   
            
            file_box.ActiveValue = LibrarySchema.FilePattern.Get();
            folder_box.ActiveValue = LibrarySchema.FolderPattern.Get(); 
            OnFolderFileChanged(null, null);

            copy_on_import.Active = LibrarySchema.CopyOnImport.Get();
            write_metadata.Active = LibrarySchema.WriteMetadata.Get();
            error_correction.Active = ImportSchema.AudioCDErrorCorrection.Get();
        }
        
        private void ConnectEvents()
        {
            library_reset.Clicked += delegate { 
                library_location_chooser.SetFilename(Paths.DefaultLibraryPath);
            };
            
            library_location_chooser.SelectionChanged += delegate {
                Paths.LibraryLocation = library_location_chooser.Filename;
                //InterfaceElements.PlaylistView.QueueDraw();
            };
            
            copy_on_import.Toggled += delegate {
                LibrarySchema.CopyOnImport.Set(copy_on_import.Active);
            };
            
            write_metadata.Toggled += delegate {
                LibrarySchema.WriteMetadata.Set(write_metadata.Active);
            };

            error_correction.Toggled += delegate {
                ImportSchema.AudioCDErrorCorrection.Set(error_correction.Active);
            };
            
            folder_box.Changed += OnFolderFileChanged;
            file_box.Changed += OnFolderFileChanged;
        }
        
        private void OnFolderFileChanged(object o, EventArgs args)
        {
            (Glade["example_path"] as Label).Markup = String.Format("<small><i>{0}.ogg</i></small>",
                GLib.Markup.EscapeText(FileNamePattern.CreateFromTrackInfo(
                    FileNamePattern.CreateFolderFilePattern(folder_box.ActiveValue, file_box.ActiveValue),
                    new SampleTrackInfo())));
                        
            LibrarySchema.FilePattern.Set(file_box.ActiveValue);
            LibrarySchema.FolderPattern.Set(folder_box.ActiveValue);
        }
    }
}
