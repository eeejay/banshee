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
using Mono.Unix;
using Gtk;

using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Equalizer;

namespace Banshee.Equalizer.Gui
{
    public class EqualizerWindow : Window
    {
        private EqualizerView eq_view;
        private EqualizerPresetComboBox eq_preset_combo;
        private CheckButton eq_enabled_checkbox;
        private HBox header_box;
        
        private static EqualizerWindow instance;
        public static EqualizerWindow Instance {
            get { return instance; }
        }
        
        public EqualizerWindow (Window parent) : base (Catalog.GetString ("Equalizer"))
        {
            if (instance == null) {
                instance = this;
            }
            
            TransientFor = parent;
            WindowPosition = WindowPosition.CenterOnParent;
            TypeHint = Gdk.WindowTypeHint.Dialog;
            SkipPagerHint = true;
            SkipTaskbarHint = true;
            AppPaintable = true;
            
            SetDefaultSize (-1, 180);
            
            VBox box = new VBox ();
            header_box = new HBox ();
            header_box.BorderWidth = 4;
            header_box.Spacing = 2;
            
            box.PackStart (header_box, false, false, 0);
            box.PackStart (new HSeparator (), false, false, 0);
        
            eq_view = new EqualizerView ();
            eq_view.BorderWidth = 10;
            eq_view.SetSizeRequest (-1, 110);
            eq_view.Frequencies = ((IEqualizer)ServiceManager.PlayerEngine.ActiveEngine).EqualizerFrequencies;
            eq_view.Show ();
            
            eq_enabled_checkbox = new CheckButton (Catalog.GetString ("Enabled"));
            
            eq_preset_combo = new EqualizerPresetComboBox ();
            eq_preset_combo.Changed += OnPresetChanged;
            eq_preset_combo.Show ();
            
            Button new_preset_button = new Button (new Image (Stock.Add, IconSize.Button));
            new_preset_button.Relief = ReliefStyle.None;
            new_preset_button.Clicked += OnNewPreset;
            
            Button delete_preset_button = new Button (new Image (Stock.Remove, IconSize.Button));
            delete_preset_button.Relief = ReliefStyle.None;
            delete_preset_button.Clicked += OnDeletePreset;
            
            VBox combo_box = new VBox ();
            combo_box.PackStart (eq_preset_combo, true, false, 0);
            
            header_box.PackStart (combo_box, false, false, 0);
            header_box.PackStart (new_preset_button, false, false, 0);
            header_box.PackStart (delete_preset_button, false, false, 0);
            header_box.PackEnd (eq_enabled_checkbox, false, false, 0);
            
            box.PackStart (eq_view, true, true, 0);
            
            eq_enabled_checkbox.Active = EqualizerSetting.EnabledSchema.Get ();
            eq_preset_combo.ActivatePreferredEqualizer (EqualizerSetting.PresetSchema.Get ());
            
            if (eq_enabled_checkbox.Active) {
                // enable equalizer if was enabled last session
                EqualizerManager.Instance.Enable (eq_preset_combo.ActiveEqualizer);
            }
            
            if (eq_preset_combo.ActiveEqualizer == null) {
                // user has no presets, so create one
                OnNewPreset (null, null);
                
                // enable our new preset (it has no effect though, since all bands are 0db)
                eq_enabled_checkbox.Active = true;
                OnEnableDisable (null, null);
            }
            
            eq_enabled_checkbox.Clicked += OnEnableDisable;
            
            Gdk.Geometry limits = new Gdk.Geometry ();
            limits.MinWidth = -1;
            limits.MaxWidth = -1;
            limits.MinHeight = SizeRequest ().Height;
            limits.MaxHeight = Gdk.Screen.Default.Height;
            SetGeometryHints (this, limits, Gdk.WindowHints.MaxSize);

            KeyPressEvent += OnKeyPress;

            Add (box);
            box.ShowAll ();
        }

        protected void OnKeyPress (object o, Gtk.KeyPressEventArgs evnt)
        {
            if (evnt.Event.Key == Gdk.Key.Escape) {
                Destroy ();
            }
        }

        protected override void OnDestroyed ()
        {
            instance = null;
            base.OnDestroyed ();
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            GdkWindow.DrawRectangle (Style.BackgroundGC (StateType.Active), true, header_box.Allocation);
            return base.OnExposeEvent (evnt);
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
            if (eq_preset_combo.ActiveEqualizer != eq_view.EqualizerSetting) {
                eq_view.EqualizerSetting = eq_preset_combo.ActiveEqualizer;
                OnEnableDisable (null, null);
            }
        }
        
        private void OnEnableDisable (object o, EventArgs args)
        {
            if (eq_enabled_checkbox.Active) {
                EqualizerManager.Instance.Enable (eq_preset_combo.ActiveEqualizer);
            } else {
                EqualizerManager.Instance.Disable (eq_preset_combo.ActiveEqualizer);
            }
        }
    }
}
