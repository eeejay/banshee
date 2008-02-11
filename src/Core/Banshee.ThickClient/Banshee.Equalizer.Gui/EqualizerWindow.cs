//
// EqualizerWindow.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
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
using System.Collections;
using Mono.Unix;
using Gtk;
using Glade;

using Banshee.Base;
using Banshee.Gui.Dialogs;
using Banshee.Gui.Widgets;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Equalizer;

namespace Banshee.Equalizer.Gui
{
    public class EqualizerWindow : GladeWindow
    {
        private EqualizerView eq_view;
        private EqualizerPresetComboBox eq_preset_combo;
        [Widget] private CheckButton eq_enabled_checkbox;
        
        public EqualizerWindow () : base ("EqualizerWindow")
        {
            eq_view = new EqualizerView ();
            eq_view.BorderWidth = 10;
            eq_view.SetSizeRequest (-1, 110);
            eq_view.Frequencies = ((IEqualizer) ServiceManager.PlayerEngine.ActiveEngine).EqualizerFrequencies;
            eq_view.Show ();
            
            eq_enabled_checkbox = (Glade["eq_enabled_checkbox"] as CheckButton);
            
            eq_preset_combo = new EqualizerPresetComboBox ();
            eq_preset_combo.Changed += OnPresetChanged;
            eq_preset_combo.Show ();
            
            Window.Realized += delegate {
                Widget header = Glade["eq_header_evbox"];
                header.ModifyBg (StateType.Normal, header.Style.Background (StateType.Active));
                Window.Show ();
            };
            
            (Glade["eq_view_box"] as Box).PackStart (eq_view, true, true, 0);
            (Glade["eq_preset_box"] as Box).PackStart (eq_preset_combo, true, false, 0);
            (Glade["new_preset_button"] as Button).Clicked += OnNewPreset;
            (Glade["delete_preset_button"] as Button).Clicked += OnDeletePreset;
            
            eq_enabled_checkbox.Active = EqualizerSetting.EnabledSchema.Get ();
            eq_preset_combo.ActivatePreferredEqualizer (EqualizerSetting.PresetSchema.Get ());
            
            if(eq_enabled_checkbox.Active) {
                // enable equalizer if was enabled last session
                EqualizerManager.Instance.Enable (eq_preset_combo.ActiveEqualizer);
            }
            
            if (eq_preset_combo.ActiveEqualizer == null) {
                // user has no presets, so create one
                OnNewPreset (null, null);
                
                // enable our new preset (it has no effect though, since all bands are 0db)
                eq_enabled_checkbox.Active = true;
                OnEnableDisable (null, null);    // notify engine and save it for next session
            }
            
            eq_enabled_checkbox.Clicked += OnEnableDisable;
        }
         
        private void OnNewPreset (object o, EventArgs args)
        {
            EqualizerSetting eq = new EqualizerSetting ("New Preset");
            EqualizerManager.Instance.Add (eq);
            eq_preset_combo.ActiveEqualizer = eq;
            eq_preset_combo.Entry.SelectRegion (0, eq_preset_combo.Entry.Text.Length);
        }
        
        private void OnDeletePreset (object o, EventArgs args)
        {
            EqualizerManager.Instance.Remove (eq_preset_combo.ActiveEqualizer);
        }
        
        private void OnPresetChanged (object o, EventArgs args)
        {
            if(eq_preset_combo.ActiveEqualizer != eq_view.EqualizerSetting) {
                eq_view.EqualizerSetting = eq_preset_combo.ActiveEqualizer;
                OnEnableDisable (null, null);
            }
        }
        
        private void OnEnableDisable (object o, EventArgs args)
        {
            if (eq_enabled_checkbox.Active) {
                EqualizerManager.Instance.Enable(eq_preset_combo.ActiveEqualizer);
            } else {
                EqualizerManager.Instance.Disable(eq_preset_combo.ActiveEqualizer);
            }
        }
    }
}
