using System;
using Gtk;
using Glade;

using Mono.Gettext;

using Banshee.Base;
using Banshee.Gui;
using Banshee.MediaEngine;
using Banshee.Widgets;
using Banshee.Plugins;
using Banshee.Configuration;
using Banshee.Configuration.Schema;

namespace Banshee.Plugins.MiniMode
{ 
    public class MiniMode : Window
    {
        [Widget] private Gtk.Box SeekContainer;
        [Widget] private Gtk.Box VolumeContainer;
        [Widget] private Gtk.Box InfoBox;
        [Widget] private Gtk.Box SourceBox;
        [Widget] private Gtk.Box CoverBox;
        [Widget] private Gtk.Box PlaybackBox;
        [Widget] private Gtk.Box LowerButtonsBox;
        
        [Widget] private Gtk.Button fullmode_button;
        
        [Widget] private Gtk.Label TitleLabel;
        [Widget] private Gtk.Label AlbumLabel;
        [Widget] private Gtk.Label ArtistLabel;
        
        private CoverArtThumbnail cover_art_thumbnail;
        private Bacon.VolumeButton volume_button;
        private SourceComboBox source_combo_box;
        private SeekSlider seek_slider;
        private StreamPositionLabel stream_position_label;
        private Tooltips toolTips;

        private Gtk.Window default_main_window;
		
        private Glade.XML glade;

        public MiniMode() : base(Branding.ApplicationLongName)
        {
            default_main_window = InterfaceElements.MainWindow;

            glade = new Glade.XML(null, "minimode.glade", "MiniModeWindow", null);
            glade.Autoconnect(this);
            
            Widget child = glade["mini_mode_contents"];
            (child.Parent as Container).Remove(child);
            Add(child);
            BorderWidth = 12;
            Resizable = false;

            IconThemeUtils.SetWindowIcon(this);
            DeleteEvent += delegate {
                Globals.ActionManager["QuitAction"].Activate();
            };
            
            // Playback Buttons
            ActionButton previous_button = new ActionButton(Globals.ActionManager["PreviousAction"]);
            previous_button.LabelVisible = false;
            previous_button.Padding = 1;
            
            ActionButton next_button = new ActionButton(Globals.ActionManager["NextAction"]);
            next_button.LabelVisible = false;
            next_button.Padding = 1;
            
            ActionButton playpause_button = new ActionButton(Globals.ActionManager["PlayPauseAction"]);
            playpause_button.LabelVisible = false;
            playpause_button.Padding = 1;
            
            PlaybackBox.PackStart(previous_button, false, false, 0);
            PlaybackBox.PackStart(playpause_button, false, false, 0);
            PlaybackBox.PackStart(next_button, false, false, 0);
            PlaybackBox.ShowAll();
            
            // Seek Slider/Position Label
            seek_slider = new SeekSlider();
            seek_slider.SetSizeRequest(125, -1);
            seek_slider.SeekRequested += delegate {
                PlayerEngineCore.Position = (uint)seek_slider.Value;
            };
            
            stream_position_label = new StreamPositionLabel(seek_slider);
            
            SeekContainer.PackStart(seek_slider, false, false, 0);
            SeekContainer.PackStart(stream_position_label, false, false, 0);
            SeekContainer.ShowAll();

            // Volume button
            volume_button = new Bacon.VolumeButton();
            VolumeContainer.PackStart(volume_button, false, false, 0);
            volume_button.Show();
            volume_button.VolumeChanged += delegate(int volume) {
                PlayerEngineCore.Volume = (ushort)volume;
                PlayerEngineCore.VolumeSchema.Set(volume);
            };
            
            // Cover
            cover_art_thumbnail = new CoverArtThumbnail(90);
            Gdk.Pixbuf default_pixbuf = Banshee.Base.Branding.DefaultCoverArt;
            cover_art_thumbnail.NoArtworkPixbuf = default_pixbuf;
            CoverBox.PackStart(cover_art_thumbnail, false, false, 0);

            // Source combobox
            source_combo_box = new SourceComboBox();
            SourceBox.PackStart(source_combo_box, true, true, 0);
            source_combo_box.ShowAll();
            
            // Repeat/Shuffle buttons
            MultiStateToggleButton shuffle_toggle_button = new MultiStateToggleButton();
            shuffle_toggle_button.AddState(typeof(ShuffleDisabledToggleState),
                    Globals.ActionManager["ShuffleAction"] as ToggleAction);
            shuffle_toggle_button.AddState(typeof(ShuffleEnabledToggleState),
                    Globals.ActionManager["ShuffleAction"] as ToggleAction);
            shuffle_toggle_button.Relief = ReliefStyle.None;
            shuffle_toggle_button.ShowLabel = false;
            try {
				shuffle_toggle_button.ActiveStateIndex = PlayerWindowSchema.PlaybackShuffle.Get() ? 1 : 0;
			} catch {
				shuffle_toggle_button.ActiveStateIndex = 0;
			}
            shuffle_toggle_button.ShowAll();
            
            MultiStateToggleButton repeat_toggle_button = new MultiStateToggleButton();
            repeat_toggle_button.AddState(typeof(RepeatNoneToggleState),
                Globals.ActionManager["RepeatNoneAction"] as ToggleAction);
            repeat_toggle_button.AddState(typeof(RepeatAllToggleState),
                Globals.ActionManager["RepeatAllAction"] as ToggleAction);
            repeat_toggle_button.AddState(typeof(RepeatSingleToggleState),
                Globals.ActionManager["RepeatSingleAction"] as ToggleAction);
            repeat_toggle_button.Relief = ReliefStyle.None;
            repeat_toggle_button.ShowLabel = false;
            try {
				repeat_toggle_button.ActiveStateIndex = (int)PlayerWindowSchema.PlaybackRepeat.Get();
			} catch {
				repeat_toggle_button.ActiveStateIndex = 0;
			}
            repeat_toggle_button.ShowAll();
            
            LowerButtonsBox.PackEnd(repeat_toggle_button, false, false, 0);
            LowerButtonsBox.PackEnd(shuffle_toggle_button, false, false, 0);
            LowerButtonsBox.ShowAll();
            
            // Tooltips
            toolTips = new Tooltips();

            SetTip(previous_button, Catalog.GetString("Play previous song"));
            SetTip(playpause_button, Catalog.GetString("Play/pause current song"));
            SetTip(next_button, Catalog.GetString("Play next song"));
            SetTip(fullmode_button, Catalog.GetString("Switch back to full mode"));
            SetTip(volume_button, Catalog.GetString("Adjust volume"));
            SetTip(repeat_toggle_button, Catalog.GetString("Change repeat playback mode"));
            SetTip(shuffle_toggle_button, Catalog.GetString("Toggle shuffle playback mode"));

            // Hook up everything
            PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
            PlayerEngineCore.StateChanged += OnPlayerEngineStateChanged;
            
            SetHeightLimit();
        }

        private void SetTip(Widget widget, string tip)
        {
	        toolTips.SetTip(widget, tip, tip);
        }

        private void SetHeightLimit()
        {
            Gdk.Geometry limits = new Gdk.Geometry();
            
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            limits.MinWidth = SizeRequest().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;

            SetGeometryHints(this, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }

        public new void Show()
        {
            source_combo_box.UpdateActiveSource();
            UpdateMetaDisplay();

            default_main_window.Hide();
            InterfaceElements.MainWindow = this;
            
            base.Show();
            
            volume_button.Volume = PlayerEngineCore.Volume;
        }

        public new void Hide()
        {
            base.Hide();
            default_main_window.Show();
            InterfaceElements.MainWindow = default_main_window;
        }
        
        public void Hide(object o, EventArgs a)
        {
            Hide();
        }

        // ---- Player Event Handlers ----
        
        private void OnPlayerEngineStateChanged(object o, Banshee.MediaEngine.PlayerEngineStateArgs args)
        {
            switch(args.State) {
                case PlayerEngineState.Loaded:
                    seek_slider.Duration = PlayerEngineCore.CurrentTrack.Duration.TotalSeconds;
                    UpdateMetaDisplay();
                    break;
                case PlayerEngineState.Idle:
                    seek_slider.SetIdle();
                    InfoBox.Visible = false;
                    UpdateMetaDisplay();
                    break;
            }
        }
        
        private void OnPlayerEngineEventChanged(object o, PlayerEngineEventArgs args)
        {
            switch(args.Event) {
                case PlayerEngineEvent.Iterate:
                    OnPlayerEngineTick();
                    break;
                case PlayerEngineEvent.StartOfStream:
                    seek_slider.CanSeek = PlayerEngineCore.CanSeek;
                    break;
                case PlayerEngineEvent.Volume:
                    volume_button.Volume = PlayerEngineCore.Volume;
                    break;
                case PlayerEngineEvent.Buffering:
                    if(args.BufferingPercent >= 1.0) {
                        stream_position_label.IsBuffering = false;
                        break;
                    }
                    
                    stream_position_label.IsBuffering = true;
                    stream_position_label.BufferingProgress = args.BufferingPercent;
                    break;
                case PlayerEngineEvent.Error:
                    UpdateMetaDisplay();
                    break;
                case PlayerEngineEvent.TrackInfoUpdated:
                    UpdateMetaDisplay();
                    break;
            }
        }

        private void OnPlayerEngineTick()
        {
            uint stream_length = PlayerEngineCore.Length;
            uint stream_position = PlayerEngineCore.Position;
            
            seek_slider.CanSeek = PlayerEngineCore.CanSeek;
            seek_slider.Duration = stream_length;
            seek_slider.SeekValue = stream_position;
        }
        
        public void UpdateMetaDisplay()
        {
            TrackInfo track = PlayerEngineCore.CurrentTrack;
            
            if(track == null) {
                Title = Branding.ApplicationLongName;
                InfoBox.Visible = false;
                return;
            }
            
            ArtistLabel.Markup = track.DisplayArtist;
            TitleLabel.Markup = String.Format("<big><b>{0}</b></big>", GLib.Markup.EscapeText(track.DisplayTitle));
            AlbumLabel.Markup = String.Format("<i>{0}</i>", GLib.Markup.EscapeText(track.DisplayAlbum));
            
            InfoBox.Visible = true;
            
            Title = track.DisplayTitle + " (" + track.DisplayArtist + ")";
            
            try {
                cover_art_thumbnail.FileName = track.CoverArtFileName;
                cover_art_thumbnail.Label = track.DisplayArtist + " - " + track.DisplayAlbum;
                SetHeightLimit();
            } catch(Exception) {
            }
        }        
    }
}

