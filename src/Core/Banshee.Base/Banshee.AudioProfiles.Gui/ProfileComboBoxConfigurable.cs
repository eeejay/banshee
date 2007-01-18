/***************************************************************************
 *  ProfileComboBoxConfigurable.cs
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
using Gtk;

namespace Banshee.AudioProfiles.Gui
{
    public class ProfileComboBoxConfigurable : VBox
    {
        private ProfileComboBox combo;
        private ProfileConfigureButton button;
        private TextView description;
        private string configuration_id;
        
        private static Gdk.Color window_color = Gdk.Color.Zero;
        
        public ProfileComboBoxConfigurable(ProfileManager manager, string configurationId) 
            : this(manager, configurationId, null)
        {
        }
        
        public ProfileComboBoxConfigurable(ProfileManager manager, string configurationId, Box parent) 
        {
            HBox editor = new HBox();
            
            configuration_id = configurationId;
            combo = new ProfileComboBox(manager);
            combo.Show();
            
            button = new ProfileConfigureButton(configurationId);
            button.ComboBox = combo;
            button.Show();
            
            editor.Spacing = 5;
            editor.PackStart(combo, true, true, 0);
            editor.PackStart(button, false, false, 0);
            editor.Show();
            
            if(window_color.Equals(Gdk.Color.Zero)) {
                Window temp = new Window("temp");
                temp.EnsureStyle();
                window_color = temp.Style.Background(StateType.Normal);
            }
            
            description = new TextView();
            description.WrapMode = WrapMode.Word;
            description.Editable = false;
            description.CursorVisible = false;
            description.ModifyBase(StateType.Normal, window_color);
            description.Show();
            
            TextTag tag = new TextTag("small");
            tag.Scale = Pango.Scale.Small;
            tag.Style = Pango.Style.Italic;
            description.Buffer.TagTable.Add(tag);
            
            Combo.SetActiveProfile(manager.GetConfiguredActiveProfile(configurationId));
            
            SetDescription();
            
            Combo.Changed += delegate {
                ProfileConfiguration.SaveActiveProfile(Combo.ActiveProfile, configurationId);
                SetDescription();
            };
            
            Spacing = 5;
            PackStart(editor, true, true, 0);
            
            bool expand = parent != null;
            
            if(parent == null) {
                parent = this;
            }
            
            parent.PackStart(description, expand, expand, 0);
        }
        
        private void SetDescription()
        {
            description.Buffer.Text = Combo.ActiveProfile.Description;
            description.Buffer.ApplyTag("small", description.Buffer.StartIter, description.Buffer.EndIter);
        }
        
        public ProfileComboBox Combo {
            get { return combo; }
        }
        
        public string ConfigurationID {
            get { return configuration_id; }
        }
    }
}
