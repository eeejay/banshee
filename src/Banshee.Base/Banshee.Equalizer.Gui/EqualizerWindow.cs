/***************************************************************************
 *  EqualizerWindow.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 *             Ivan N. Zlatev <contact i-nZ.net>
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
using Glade;
using Mono.Unix;

using Banshee.Gui;

namespace Banshee.Equalizer.Gui
{
    public class EqualizerEditor : GladeWindow
    {
        private EqualizerView eq_view;
        private EqualizerPresetComboBox eq_preset_combo;
        
        [Widget] private ComboBoxEntry preset_combobox;
        
        public EqualizerEditor() : base("EqualizerWindow")
        {
            eq_view = new EqualizerView();
            eq_view.BorderWidth = 10;
            eq_view.SetSizeRequest(-1, 110);
            eq_view.Bands = EqualizerManager.Instance.SupportedBands;
            eq_view.Show();
            
            eq_preset_combo = new EqualizerPresetComboBox();
            eq_preset_combo.Changed += OnPresetChanged;
            eq_preset_combo.ActivateFirstEqualizer();
            eq_preset_combo.Show();
            
            Window.Realized += delegate {
                Widget header = Glade["eq_header_evbox"];
                header.ModifyBg(StateType.Normal, header.Style.Background(StateType.Active));
            };
            
            (Glade["eq_view_box"] as Box).PackStart(eq_view, true, true, 0);
            (Glade["eq_preset_box"] as Box).PackStart(eq_preset_combo, true, false, 0);
            (Glade["new_preset_button"] as Button).Clicked += OnNewPreset;
            (Glade["delete_preset_button"] as Button).Clicked += OnDeletePreset;
        }
         
        private void OnNewPreset(object o, EventArgs args)
        {
            EqualizerSetting eq = new EqualizerSetting("New Preset");
            EqualizerManager.Instance.Add(eq);
            eq_preset_combo.ActiveEqualizer = eq;
            eq_preset_combo.Entry.SelectRegion(0, eq_preset_combo.Entry.Text.Length);
        }
        
        private void OnDeletePreset(object o, EventArgs args)
        {
            EqualizerManager.Instance.Remove(eq_preset_combo.ActiveEqualizer);
        }
        
        private void OnPresetChanged(object o, EventArgs args)
        {
            if(eq_preset_combo.ActiveEqualizer != eq_view.EqualizerSetting) {
                eq_view.EqualizerSetting = eq_preset_combo.ActiveEqualizer;
            }
        }
    }
}