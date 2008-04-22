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

namespace Banshee.Preferences.Gui
{
    public static class DefaultPreferenceWidgets
    {
        public static void Load (PreferenceService service)
        {
            PreferenceBase library_location = service["general"]["music-library"]["library-location"];
            library_location.DisplayWidget = new LibraryLocationButton (library_location);
        }

        private class LibraryLocationButton : HBox
        {
            private FileChooserButton chooser;
            private Button reset;
            private LibraryLocationPreference preference;
            
            public LibraryLocationButton (PreferenceBase pref)
            {
                preference = (LibraryLocationPreference)pref;
                
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
    }
}
