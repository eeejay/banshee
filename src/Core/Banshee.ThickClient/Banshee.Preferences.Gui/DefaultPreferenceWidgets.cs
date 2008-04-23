//
// DefaultPreferenceWidgets.cs
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
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Library;
using Banshee.Preferences;
using Banshee.Collection;

using Hyena.Widgets;
using Banshee.Widgets;
using Banshee.Gui.Widgets;

namespace Banshee.Preferences.Gui
{
    public static class DefaultPreferenceWidgets
    {
        public static void Load (PreferenceService service)
        {
            Page general = service["general"];
        
            PreferenceBase library_location = general["music-library"]["library-location"];
            library_location.DisplayWidget = new LibraryLocationButton (library_location);
            
            PreferenceBase folder_pattern = general["file-system"]["folder_pattern"];
            folder_pattern.DisplayWidget = new PatternComboBox (folder_pattern, FileNamePattern.SuggestedFolders);
            
            PreferenceBase file_pattern = general["file-system"]["file_pattern"];
            file_pattern.DisplayWidget = new PatternComboBox (file_pattern, FileNamePattern.SuggestedFiles);
            
            PreferenceBase pattern_display = general["file-system"].FindOrAdd (new VoidPreference ("file_folder_pattern"));
            pattern_display.DisplayWidget = new PatternDisplay (folder_pattern.DisplayWidget, file_pattern.DisplayWidget);
            
            // Set up the extensions tab UI
            AddinView view = new AddinView ();
            view.Show ();
            
            Gtk.ScrolledWindow scroll = new Gtk.ScrolledWindow ();
            scroll.HscrollbarPolicy = PolicyType.Never;
            scroll.AddWithViewport (view);
            scroll.Show ();
            
            service["extensions"].DisplayWidget = scroll;
        }

        private class LibraryLocationButton : HBox
        {
            private FileChooserButton chooser;
            private Button reset;
            private LibraryLocationPreference preference;
            
            public LibraryLocationButton (PreferenceBase pref)
            {
                preference = (LibraryLocationPreference)pref;
                preference.ShowLabel = false;
                
                Spacing = 5;
                
                chooser = new FileChooserButton (Catalog.GetString ("Select library location"), 
                    FileChooserAction.SelectFolder);
                chooser.SetCurrentFolder (preference.Value);
                chooser.SelectionChanged += OnChooserChanged;
                    
                HBox box = new HBox ();
                box.Spacing = 2;
                box.PackStart (new Image (Stock.Undo, IconSize.Button), false, false, 0);
                box.PackStart (new Label (Catalog.GetString ("Reset")), false, false, 0);
                reset = new Button ();
                reset.Clicked += OnReset;
                reset.Add (box);
                
                PackStart (chooser, true, true, 0);
                PackStart (reset, false, false, 0);
                
                chooser.Show ();
                reset.ShowAll ();
            }
            
            private void OnReset (object o, EventArgs args)
            {
                chooser.SetFilename (Paths.DefaultLibraryPath);
            }
            
            private void OnChooserChanged (object o, EventArgs args)
            {
                preference.Value = chooser.Filename;
            }
        }
        
        private class PatternComboBox : DictionaryComboBox<string>
        {
            private Preference<string> preference;
            
            public PatternComboBox (PreferenceBase pref, string [] patterns)
            {
                preference = (Preference<string>)pref;
                
                bool already_added = false;
                string conf_pattern = preference.Value;
                
                foreach (string pattern in patterns) {
                    if (!already_added && pattern.Equals (conf_pattern)) {
                        already_added = true;
                    }
                    
                    Add (FileNamePattern.CreatePatternDescription (pattern), pattern);
                }
                
                if (!already_added) {
                    Add (FileNamePattern.CreatePatternDescription (conf_pattern), conf_pattern);
                }
                
                ActiveValue = conf_pattern;
            }
            
            protected override void OnChanged ()
            {
                preference.Value = ActiveValue;
                base.OnChanged ();
            }
        }
        
        private class PatternDisplay : WrapLabel
        {
            private PatternComboBox folder;
            private PatternComboBox file;
            
            private SampleTrackInfo track = new SampleTrackInfo ();
            
            public PatternDisplay (object a, object b)
            {
                folder= (PatternComboBox)a;
                file = (PatternComboBox)b;
                
                folder.Changed += OnChanged;
                file.Changed += OnChanged;
                
                OnChanged (null, null);
            }

            private void OnChanged (object o, EventArgs args)
            {
                string display = FileNamePattern.CreateFromTrackInfo (FileNamePattern.CreateFolderFilePattern (
                    folder.ActiveValue, file.ActiveValue), track);
            
                Markup = String.IsNullOrEmpty (display) ? String.Empty : String.Format ("<small>{0}.ogg</small>", 
                    GLib.Markup.EscapeText (display));
            }
        }
    }
}
