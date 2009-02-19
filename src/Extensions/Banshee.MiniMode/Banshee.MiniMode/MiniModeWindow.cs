/***************************************************************************
 *  MiniModeWindow.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 *             Felipe Almeida Lessa
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

using Hyena.Widgets;
using Hyena.Gui;

using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Sources.Gui;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Widgets;

namespace Banshee.MiniMode
{ 
    public class MiniMode : Banshee.Gui.BaseClientWindow
    {
        [Widget] private Gtk.Box SeekContainer;
        [Widget] private Gtk.Box VolumeContainer;
        [Widget] private Gtk.Box InfoBox;
        [Widget] private Gtk.Box SourceBox;
        [Widget] private Gtk.Box CoverBox;
        [Widget] private Gtk.Box PlaybackBox;
        [Widget] private Gtk.Box LowerButtonsBox;
        
        [Widget] private Gtk.Button fullmode_button;
        
        private TrackInfoDisplay track_info_display;
        private ConnectedVolumeButton volume_button;
        private SourceComboBox source_combo_box;
        private ConnectedSeekSlider seek_slider;
        private object tooltip_host;

        private BaseClientWindow default_main_window;

        private Glade.XML glade;

        public MiniMode (BaseClientWindow defaultMainWindow) : base (Catalog.GetString ("Banshee Media Player"), "minimode", 0, 0)
        {
            default_main_window = defaultMainWindow;
            
            glade = new Glade.XML (System.Reflection.Assembly.GetExecutingAssembly (), "minimode.glade", "MiniModeWindow", null);
            glade.Autoconnect (this);
            
            Widget child = glade["mini_mode_contents"];
            (child.Parent as Container).Remove (child);
            Add (child);
            BorderWidth = 12;
            Resizable = false;

            // Playback Buttons
            Widget previous_button = ActionService.PlaybackActions["PreviousAction"].CreateToolItem ();
            
            Widget playpause_button = ActionService.PlaybackActions["PlayPauseAction"].CreateToolItem ();
            
            Widget button = ActionService.PlaybackActions["NextAction"].CreateToolItem ();
            Menu menu = ActionService.PlaybackActions.ShuffleActions.CreateMenu ();
            MenuButton next_button = new MenuButton (button, menu, true);
            
            PlaybackBox.PackStart (previous_button, false, false, 0);
            PlaybackBox.PackStart (playpause_button, false, false, 0);
            PlaybackBox.PackStart (next_button, false, false, 0);
            PlaybackBox.ShowAll ();
            
            // Seek Slider/Position Label
            seek_slider = new ConnectedSeekSlider ();
            
            SeekContainer.PackStart (seek_slider, false, false, 0);
            SeekContainer.ShowAll ();

            // Volume button
            volume_button = new ConnectedVolumeButton ();
            VolumeContainer.PackStart (volume_button, false, false, 0);
            volume_button.Show ();
            
            // Source combobox
            source_combo_box = new SourceComboBox ();
            SourceBox.PackStart (source_combo_box, true, true, 0);
            source_combo_box.Show ();
            
            // Track info
            track_info_display = new ClassicTrackInfoDisplay ();
            track_info_display.Show ();
            CoverBox.PackStart (track_info_display, true, true, 0);

            // Repeat button
            RepeatActionButton repeat_toggle_button = new RepeatActionButton ();
            
            LowerButtonsBox.PackEnd (repeat_toggle_button, false, false, 0);
            LowerButtonsBox.ShowAll ();
            
            tooltip_host = TooltipSetter.CreateHost ();

            SetTip (fullmode_button, Catalog.GetString ("Switch back to full mode"));
            SetTip (repeat_toggle_button, Catalog.GetString ("Change repeat playback mode"));
            
            // Hook up everything
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, 
                PlayerEvent.Error |
                PlayerEvent.StateChange |
                PlayerEvent.TrackInfoUpdated);
            
            SetHeightLimit ();
        }

        protected override void Initialize ()
        {
        }

        private void SetTip (Widget widget, string tip)
        {
            TooltipSetter.Set (tooltip_host, widget, tip);
        }

        private void SetHeightLimit ()
        {
            Gdk.Geometry limits = new Gdk.Geometry ();
            
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            limits.MinWidth = SizeRequest ().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;

            SetGeometryHints (this, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }

        public void Enable ()
        {
            source_combo_box.UpdateActiveSource ();
            UpdateMetaDisplay ();

            default_main_window.Hide ();

            Show ();
        }

        public void Disable ()
        {
            Hide ();
            default_main_window.Show ();
        }

        // Called when the user clicks the fullmode_button
        public void Hide (object o, EventArgs a)
        {
            ElementsService.PrimaryWindow = default_main_window;
            Disable ();
        }

        // ---- Player Event Handlers ----
        
        private void OnPlayerEvent (PlayerEventArgs args)
        {
            switch (args.Event) {
                case PlayerEvent.Error:
                case PlayerEvent.TrackInfoUpdated:
                    UpdateMetaDisplay ();
                    break;
                case PlayerEvent.StateChange:
                    switch (((PlayerEventStateChangeArgs)args).Current) {
                        case PlayerState.Loaded:
                            UpdateMetaDisplay ();
                            break;
                        case PlayerState.Idle:
                            InfoBox.Visible = false;
                            UpdateMetaDisplay ();
                            break;
                    }
                    break;
            }
        }

        protected void UpdateMetaDisplay ()
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
            
            if (track == null) {
                InfoBox.Visible = false;
                return;
            }
            
            InfoBox.Visible = true;
            
            try {
                SetHeightLimit ();
            } catch (Exception) {
            }
        }        
    }
}

