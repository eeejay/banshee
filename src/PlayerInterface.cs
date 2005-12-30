/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PlayerInterface.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Threading;
using System.Data;
using System.Collections;
using System.Reflection;
using Mono.Unix;
using Gtk;
using Gdk;
using Glade;
using System.IO;

using Sql;
using Banshee.Widgets;
using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.Dap;

namespace Banshee
{
    public class PlayerUI
    {
        public static readonly uint SkipDelta = 10;
        public static readonly int VolumeDelta = 10;
        
        private Glade.XML gxml;

        [Widget] private Gtk.Window WindowPlayer;
        [Widget] private Gtk.HScale ScaleTime;
        [Widget] private Gtk.Label LabelInfo;
        [Widget] private HPaned SourceSplitter;
        [Widget] private Button HeaderCycleButton;

        private PlaylistModel playlistModel;

        private Label LabelStatusBar;
        private VolumeButton volumeButton;
        private PlaylistView playlistView;
        private SourceView sourceView;
        private TrackInfo activeTrackInfo;
        private NotificationAreaIconContainer trayIcon;
        private ImageAnimation spinner;
        private LibraryTransactionStatus libraryTransactionStatus;
        private TrackInfoHeader trackInfoHeader;
        private CoverArtView cover_art_view;
        private SimpleNotebook headerNotebook;
        private SearchEntry searchEntry;
        private Tooltips toolTips;
        private Hashtable playlistMenuMap;
        private Viewport sourceViewLoadingVP;
        
        private MultiStateToggleButton repeat_toggle_button;
        private MultiStateToggleButton shuffle_toggle_button;
                
        private ActionButton playpause_button;
        private ActionButton next_button;
        private ActionButton previous_button;
        private ActionButton burn_button;
        private ActionButton rip_button;
        
        private ActionButton sync_dap_button;
        private EventBox syncing_container;
        private Gtk.Image dap_syncing_image = new Gtk.Image();
        [Widget] private ProgressBar dapDiskUsageBar;
        
        private HighlightMessageArea audiocd_statusbar;
        
        private bool incrementedCurrentSongPlayCount;
    
        public Gtk.Window Window {
            get {
                return WindowPlayer;
            }
        }
        
        private long plLoaderMax, plLoaderCount;
        private bool startupLoadReady = false;
        private bool tickFromEngine = false;
        private uint setPositionTimeoutId;
        private bool updateEnginePosition = true;
        private int clickX, clickY;

        private int dapDiskUsageTextViewState;
        
        private SpecialKeys special_keys;

        private static TargetEntry [] playlistViewSourceEntries = 
            new TargetEntry [] {
                Dnd.TargetPlaylistRows,
                Dnd.TargetLibraryTrackIds,
                Dnd.TargetUriList
            };
            
        private static TargetEntry [] playlistViewDestEntries = 
            new TargetEntry [] {
                Dnd.TargetPlaylistRows,
                Dnd.TargetUriList
            };

        private static TargetEntry [] sourceViewSourceEntries = 
            new TargetEntry [] {
                Dnd.TargetSource
            };
            
        private static TargetEntry [] sourceViewDestEntries = 
            new TargetEntry [] {
                Dnd.TargetLibraryTrackIds,
            };

        private RemotePlayer banshee_dbus_object;

        public PlayerUI() 
        {
            Catalog.Init(ConfigureDefines.GETTEXT_PACKAGE, ConfigureDefines.LOCALE_DIR);
        
            gxml = new Glade.XML(null, "banshee.glade", "WindowPlayer", null);
            gxml.Autoconnect(this);
            
            ResizeMoveWindow();
            BuildWindow();   
            InstallTrayIcon();
            
            banshee_dbus_object = new RemotePlayer(Window, this);
            DBusRemote.RegisterObject(banshee_dbus_object, "Player");
            
            PlayerEngineCore.ActivePlayer.Iterate += OnPlayerTick;
            PlayerEngineCore.ActivePlayer.EndOfStream += OnPlayerEos;    
            
            if(PlayerEngineCore.ActivePlayer != PlayerEngineCore.AudioCdPlayer) {
                PlayerEngineCore.AudioCdPlayer.Iterate += OnPlayerTick;
                PlayerEngineCore.AudioCdPlayer.EndOfStream += OnPlayerEos;    
            }
            
            if(Globals.AudioCdCore != null) {
                Globals.AudioCdCore.DiskRemoved += OnAudioCdCoreDiskRemoved;
                Globals.AudioCdCore.Updated += OnAudioCdCoreUpdated;
            }
            
            DapCore.DapAdded += OnDapCoreDeviceAdded;
            
            LogCore.Instance.Updated += OnLogCoreUpdated;
            
            ImportManager.Instance.ImportRequested += OnImportManagerImportRequested;
            
            //InitialLoadTimeout();
            GLib.Timeout.Add(500, InitialLoadTimeout);
            WindowPlayer.Show();
            
            /*try {
                if((bool)Globals.Configuration.Get(GConfKeys.EnableSpecialKeys)) {
                    special_keys = new SpecialKeys();
                    special_keys.Delay = new TimeSpan(350 * TimeSpan.TicksPerMillisecond);
                    special_keys.RegisterHandler(OnSpecialKeysPressed, 
                        SpecialKey.AudioPlay,
                        SpecialKey.AudioPrev,
                        SpecialKey.AudioNext
                    );
                }
            } catch(GConf.NoSuchKeyException) {
            } catch(Exception e) {
                special_keys = null;
                LogCore.Instance.PushWarning(Catalog.GetString("Could not setup special keys"), e.Message, false);
            }*/
            
            // Bind available methods to actions defined in ActionManager
            Globals.ActionManager.DapActions.Visible = false;
            Globals.ActionManager.AudioCdActions.Visible = false;
            Globals.ActionManager.SourceEjectActions.Visible = false;
            Globals.ActionManager.SongActions.Sensitive = false;
            Globals.ActionManager.PlaylistActions.Sensitive = false;
            
            foreach(Action action in Globals.ActionManager) {
                string method_name = "On" + action.Name;
                MethodInfo method = GetType().GetMethod(method_name,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
                
                EventInfo action_event = action.GetType().GetEvent("Activated");
                 
                if(method == null || action_event == null) {
                    //Console.WriteLine("No method defined for action `{0}'", action.Name);
                    continue;
                }
                
                action_event.AddEventHandler(action, 
                    Delegate.CreateDelegate(typeof(EventHandler), this, method_name));
            }
            
            LoadSettings();
        }
   
        private bool InitialLoadTimeout()
        {
            ConnectToLibraryTransactionManager();
            Globals.Library.Reloaded += OnLibraryReloaded;
            Globals.Library.ReloadLibrary();
            
            foreach(DapDevice device in DapCore.Devices) {
                 device.PropertiesChanged += OnDapPropertiesChanged;
                 device.SaveStarted += OnDapSaveStarted;
                 device.SaveFinished += OnDapSaveFinished;
            }
            
            return false;
        }
          
        // ---- Setup/Initialization Routines ----
          
        private void ResizeMoveWindow()
        {
            int x = 0, y = 0, width = 0, height = 0;
              
            try {
                x = (int)Globals.Configuration.Get(GConfKeys.WindowX);
                y = (int)Globals.Configuration.Get(GConfKeys.WindowY); 
                width = (int)Globals.Configuration.Get(GConfKeys.WindowWidth);
                height = (int)Globals.Configuration.Get(GConfKeys.WindowHeight);
            } catch(GConf.NoSuchKeyException) {
                width = 800;
                height = 600;
                x = 0;
                y = 0;
            }

            if(width != 0 && height != 0) {
                WindowPlayer.Resize(width, height);
            }

            if(x == 0 && y == 0) {
                WindowPlayer.SetPosition(Gtk.WindowPosition.Center);
            } else {
                WindowPlayer.Move(x, y);
            }
            
            try {
                if((bool)Globals.Configuration.Get(GConfKeys.WindowMaximized)) {
                    WindowPlayer.Maximize();
                } else {
                    WindowPlayer.Unmaximize();
                }
            } catch(GConf.NoSuchKeyException) {
            }
        }
        
        private void OnWindowStateEvent(object o, WindowStateEventArgs args)
        {
            if((args.Event.NewWindowState & Gdk.WindowState.Withdrawn) == 0) {
                Globals.Configuration.Set(GConfKeys.WindowMaximized,
                    (args.Event.NewWindowState & Gdk.WindowState.Maximized) != 0);
            }
        }
          
        private void BuildWindow()
        {
            IconThemeUtils.SetWindowIcon(WindowPlayer);

            // Main Menu
            (gxml["MainMenuContainer"] as Container).Add(Globals.ActionManager.GetWidget("/MainMenu"));
      
            // Playback Buttons
            HBox playback_box = gxml["LeftToolbarContainer"] as HBox;
            
            previous_button = new ActionButton(Globals.ActionManager["PreviousAction"]);
            previous_button.LabelVisible = false;
            previous_button.Padding = 1;
            previous_button.ButtonPressEvent += OnButtonPreviousPressed;
            
            next_button = new ActionButton(Globals.ActionManager["NextAction"]);
            next_button.LabelVisible = false;
            next_button.Padding = 1;
            
            playpause_button = new ActionButton(Globals.ActionManager["PlayPauseAction"]);
            playpause_button.LabelVisible = false;
            playpause_button.Padding = 1;
            
            playback_box.PackStart(previous_button, false, false, 0);
            playback_box.PackStart(playpause_button, false, false, 0);
            playback_box.PackStart(next_button, false, false, 0);
      
            // Header
            headerNotebook = new SimpleNotebook();
            headerNotebook.Show();
            headerNotebook.PageCountChanged += OnHeaderPageCountChanged;
            ((HBox)gxml["HeaderBox"]).PackStart(headerNotebook, true, true, 0);
            
            trackInfoHeader = new TrackInfoHeader();
            trackInfoHeader.Show();
            headerNotebook.AddPage(trackInfoHeader, true);
            
            HeaderCycleButton.Visible = false;
            HeaderCycleButton.Clicked += OnHeaderCycleButtonClicked;
            
            // Burn Button
            burn_button = new ActionButton(Globals.ActionManager["WriteCDAction"]);
            burn_button.Pixbuf = Gdk.Pixbuf.LoadFromResource("cd-action-burn-24.png");
            (gxml["RightToolbarContainer"] as Box).PackStart(burn_button, false, false, 0);
                        
            // Rip Button
            rip_button = new ActionButton(Globals.ActionManager["ImportCDAction"]);
            rip_button.Pixbuf = Gdk.Pixbuf.LoadFromResource("cd-action-rip-24.png");
            (gxml["RightToolbarContainer"] as Box).PackStart(rip_button, false, false, 0);
            
            sync_dap_button = new ActionButton(Globals.ActionManager["SyncDapAction"]);
            (gxml["RightToolbarContainer"] as Box).PackStart(sync_dap_button, false, false, 0);
            
            // Volume Button
            volumeButton = new VolumeButton();
            (gxml["RightToolbarContainer"] as Box).PackStart(volumeButton, false, false, 0);
            volumeButton.Show();
            volumeButton.VolumeChanged += OnVolumeScaleChanged;
            
            // Footer 
            audiocd_statusbar = new HighlightMessageArea();
            audiocd_statusbar.BorderWidth = 5;
            audiocd_statusbar.LeftPadding = 15;
            audiocd_statusbar.ButtonClicked += OnAudioCdStatusBarButtonClicked;
            
            (gxml["MainContainer"] as Box).PackStart(audiocd_statusbar, false, false, 0);
            
            LabelStatusBar = new Label(Catalog.GetString("Banshee Music Player"));
            LabelStatusBar.Show();
            
            ActionToggleButton shuffle_button = new ActionToggleButton(Globals.ActionManager["ShuffleAction"]);
            shuffle_button.IconSize = IconSize.Menu;
            shuffle_button.Relief = ReliefStyle.None;
            shuffle_button.ShowAll();

            repeat_toggle_button = new MultiStateToggleButton();
            repeat_toggle_button.AddState(typeof(RepeatNoneToggleState),
                Globals.ActionManager["RepeatNoneAction"] as ToggleAction);
            repeat_toggle_button.AddState(typeof(RepeatAllToggleState),
                Globals.ActionManager["RepeatAllAction"] as ToggleAction);
            repeat_toggle_button.AddState(typeof(RepeatSingleToggleState),
                Globals.ActionManager["RepeatSingleAction"] as ToggleAction);
            repeat_toggle_button.Relief = ReliefStyle.None;
            repeat_toggle_button.ShowLabel = false;
            repeat_toggle_button.ShowAll();
            
            ActionButton song_properties_button = new ActionButton(Globals.ActionManager["PropertiesAction"]);
            song_properties_button.IconSize = IconSize.Menu;
            song_properties_button.Relief = ReliefStyle.None;
            song_properties_button.LabelVisible = false;
            song_properties_button.ShowAll();
            
            (gxml["BottomToolbar"] as Box).PackStart(shuffle_button, false, false, 0);
            (gxml["BottomToolbar"] as Box).PackStart(repeat_toggle_button, false, false, 0);
            (gxml["BottomToolbar"] as Box).PackStart(LabelStatusBar, true, true, 0);
            (gxml["BottomToolbar"] as Box).PackStart(song_properties_button, false, false, 0);
            
            // Cover Art View
            
            cover_art_view = new CoverArtView();
            cover_art_view.Hide();
            (gxml["LeftContainer"] as Box).PackStart(cover_art_view, false, false, 0);
            
            // Source View
            Label sourceViewLoading = new Label();
            sourceViewLoading.Yalign = 0.15f;
            sourceViewLoading.Xalign = 0.5f;
            sourceViewLoading.Markup = "<big><i>" + Catalog.GetString("Loading...") + "</i></big>";
            sourceViewLoadingVP = new Viewport();
            sourceViewLoadingVP.ShadowType = ShadowType.None;
            sourceViewLoadingVP.Add(sourceViewLoading);
            sourceViewLoadingVP.ShowAll();
            ((Gtk.ScrolledWindow)gxml["SourceContainer"]).Add(sourceViewLoadingVP);
            
            sourceView = new SourceView();
            sourceView.SourceChanged += OnSourceChanged;
            sourceView.ButtonPressEvent += OnSourceViewButtonPressEvent;
            sourceView.Sensitive = false;

            /*sourceView.EnableModelDragSource(
                Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                sourceViewSourceEntries, 
                DragAction.Copy | DragAction.Move);*/
        
            sourceView.EnableModelDragDest(
                sourceViewDestEntries, 
                DragAction.Copy | DragAction.Move);

            // Playlist View
            playlistModel = new PlaylistModel();
            playlistView = new PlaylistView(playlistModel);
            ((Gtk.ScrolledWindow)gxml["LibraryContainer"]).Add(playlistView);
            playlistView.Show();
            playlistModel.Updated += OnPlaylistUpdated;
            playlistView.KeyPressEvent += OnPlaylistViewKeyPressEvent;
            playlistView.ButtonPressEvent += OnPlaylistViewButtonPressEvent;
            playlistView.MotionNotifyEvent += OnPlaylistViewMotionNotifyEvent;
            playlistView.ButtonReleaseEvent += OnPlaylistViewButtonReleaseEvent;
            playlistView.DragDataReceived += OnPlaylistViewDragDataReceived;
            playlistView.DragDataGet += OnPlaylistViewDragDataGet;
            playlistView.DragDrop += OnPlaylistViewDragDrop;
            playlistView.Selection.Changed += OnPlaylistViewSelectionChanged;
                
            sourceView.SelectLibrary();
                
            playlistView.EnableModelDragSource(
                Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                playlistViewSourceEntries, 
                DragAction.Copy | DragAction.Move);
        
            playlistView.EnableModelDragDest( 
                playlistViewDestEntries, 
                DragAction.Copy | DragAction.Move);
            
            (gxml["LeftContainer"] as VBox).PackStart(new ActiveUserEventsManager(), false, false, 0);

            // Misc
            SetInfoLabel(Catalog.GetString("Idle"));

            // Window Events
            WindowPlayer.KeyPressEvent += OnKeyPressEvent;
            WindowPlayer.ConfigureEvent += OnWindowPlayerConfigureEvent;
            WindowPlayer.WindowStateEvent += OnWindowStateEvent;
            
            // Search Entry
            ArrayList fields = new ArrayList();
            fields.Add(Catalog.GetString("All"));
            fields.Add("-");
            fields.Add(Catalog.GetString("Song Name"));
            fields.Add(Catalog.GetString("Artist Name"));
            fields.Add(Catalog.GetString("Album Title"));
            
            searchEntry = new SearchEntry(fields);
            searchEntry.EnterPress += OnSimpleSearch;
            searchEntry.Changed += OnSimpleSearch;
            searchEntry.Show();
            ((HBox)gxml["PlaylistHeaderBox"]).PackStart(searchEntry, false, false, 0);
                
            gxml["SearchLabel"].Sensitive = false;
            searchEntry.Sensitive = false;
                
            // Repeat/Shuffle buttons
            
            /* shuffle_toggle_button = new MultiStateToggleButton();
            shuffle_toggle_button.AddState(typeof(ShuffleDisabledToggleState), gxml["ItemShuffle"] as CheckMenuItem, false);
            shuffle_toggle_button.AddState(typeof(ShuffleEnabledToggleState), gxml["ItemShuffle"] as CheckMenuItem, true);
            shuffle_toggle_button.Relief = ReliefStyle.None;
            shuffle_toggle_button.ShowLabel = false;
            shuffle_toggle_button.Changed += delegate(object o, ToggleStateChangedArgs args) {
                //HandleShuffleToggleButton();
            };
            shuffle_toggle_button.ShowAll();*/
                
            toolTips = new Tooltips();
            
            SetTip(burn_button, Catalog.GetString("Write selection to CD"));
            SetTip(rip_button, Catalog.GetString("Import CD into library"));
            SetTip(previous_button, Catalog.GetString("Play previous song"));
            SetTip(playpause_button, Catalog.GetString("Play/pause current song"));
            SetTip(next_button, Catalog.GetString("Play next song"));
            SetTip(gxml["ScaleTime"], Catalog.GetString("Current position in song"));
            SetTip(dapDiskUsageBar, Catalog.GetString("Device disk usage"));
            SetTip(sync_dap_button, Catalog.GetString("Synchronize music library to device"));
            SetTip(volumeButton, Catalog.GetString("Adjust volume"));
            
            playlistMenuMap = new Hashtable();
        }
        
        private void SetTip(Widget widget, string tip)
        {
            toolTips.SetTip(widget, tip, tip);
        }
          
        private void InstallTrayIcon()
        {
            try {
                if(!(bool)Globals.Configuration.Get(GConfKeys.ShowNotificationAreaIcon)) {
                    return;
                }
            } catch(Exception) { }
                
            try {
                trayIcon = new NotificationAreaIconContainer();
                trayIcon.ClickEvent += OnTrayClick;
                trayIcon.MouseScrollEvent += OnTrayScroll;
            } catch(Exception e) {
                trayIcon = null;
                LogCore.Instance.PushWarning(Catalog.GetString("Notification Area Icon could not be installed"),
                    e.Message, false);
            }
        }
    
        private void LoadSettings()
        {    
            try {
                volumeButton.Volume = (int)Globals.Configuration.Get(GConfKeys.Volume);
            } catch(GConf.NoSuchKeyException) {
                volumeButton.Volume = 80;
            }

            PlayerEngineCore.ActivePlayer.Volume = (ushort)volumeButton.Volume;
            if(PlayerEngineCore.AudioCdPlayer != PlayerEngineCore.ActivePlayer) {
                PlayerEngineCore.AudioCdPlayer.Volume = (ushort)volumeButton.Volume;
            }
            
            try {
                int state = (int)Globals.Configuration.Get(GConfKeys.PlaylistRepeat);
                
                foreach(RadioAction radio in (Globals.ActionManager["RepeatAllAction"] as RadioAction).Group) {
                    if(radio.Value == state) {
                        radio.Active = true;
                        break;
                    }
                }
            } catch(Exception) {}
            
            try {
                (Globals.ActionManager["ShuffleAction"] as ToggleAction).Active = 
                    (bool)Globals.Configuration.Get(GConfKeys.PlaylistShuffle);
            } catch(Exception) {}
            
            try {
                bool active = (bool)Globals.Configuration.Get(GConfKeys.ShowCoverArt);
                cover_art_view.Enabled = active;
                (Globals.ActionManager["ShowCoverArtAction"] as ToggleAction).Active = active;
            } catch(Exception) {}
            
            try {
                SourceSplitter.Position = (int)Globals.Configuration.Get(GConfKeys.SourceViewWidth);
            } catch(GConf.NoSuchKeyException) {
                SourceSplitter.Position = 125;
            }
        }
        
        private void PromptForImport()
        {
           HigMessageDialog md = new HigMessageDialog(WindowPlayer, 
                DialogFlags.Modal, MessageType.Question,
                Catalog.GetString("Import Music"),
                Catalog.GetString("Your music library is empty. You may import new music into " +
                "your library now, or choose to do so later.\n\nAutomatic import " +
                "or importing a large folder may take a long time, so please " +
                "be patient."),
                Catalog.GetString("Import Folder"));
                
            md.AddButton(Catalog.GetString("Automatic Import"), ResponseType.Apply, true);
            md.Response += OnPromptForImportResponse;
            md.ShowAll();
        }
        
        private void OnPromptForImportResponse(object o, ResponseArgs args)
        {
            (o as Dialog).Response -= OnPromptForImportResponse;
            (o as Dialog).Destroy();
            
            switch(args.ResponseId) {
                case ResponseType.Ok:
                    ImportWithFileSelector();
                    break;
                case ResponseType.Apply:
                    ImportHomeDirectory();
                    break;
            }
        }
        
        private void ConnectToLibraryTransactionManager()
        {
            PlayerCore.TransactionManager.ExecutionStackChanged += OnLTMExecutionStackChanged;
            PlayerCore.TransactionManager.ExecutionStackEmpty += OnLTMExecutionStackEmpty;
        }
        
        private void OnLTMExecutionStackChanged(object o, EventArgs args)
        {    
            if(libraryTransactionStatus == null) {
                libraryTransactionStatus = new LibraryTransactionStatus();
                libraryTransactionStatus.Stopped += OnLibraryTransactionStatusStopped;
            }
            
            if(libraryTransactionStatus.AllowShow) {
                headerNotebook.AddPage(libraryTransactionStatus, true);    
                libraryTransactionStatus.Start();
            }
        }
        
        private void OnLTMExecutionStackEmpty(object o, EventArgs args)
        {
        }
        
        public void SelectAudioCd(string device)
        {
            for(int i = 0, n = ((ListStore)sourceView.Model).IterNChildren(); i < n; i++) {
                TreeIter iter = TreeIter.Zero;
                if(!((ListStore)sourceView.Model).IterNthChild(out iter, i)) {
                    continue;
                }
                
                AudioCdSource source = ((ListStore)sourceView.Model).GetValue(iter, 0) as AudioCdSource;
                if(source == null) {
                    continue;
                }
                
                if(source.Disk.DeviceNode == device || source.Disk.Udi == device) {
                    sourceView.SelectSource(source);
                    return;
                }
            }
            
            sourceView.SelectLibraryForce();
        }
        
        public void SelectDap(string device)
        {
            for(int i = 0, n = ((ListStore)sourceView.Model).IterNChildren(); i < n; i++) {
                TreeIter iter = TreeIter.Zero;
                if(!((ListStore)sourceView.Model).IterNthChild(out iter, i)) {
                    continue;
                }
                
                DapSource source = ((ListStore)sourceView.Model).GetValue(iter, 0) as DapSource;
                if(source == null) {
                    continue;
                }
                
                if(source.Device.HalUdi == device) {
                    sourceView.SelectSource(source);
                    return;
                }
            }
            
            sourceView.SelectLibraryForce();
        }
        
        private void LoadSourceView()
        {        
            sourceView.Sensitive = true;
            ((Gtk.ScrolledWindow)gxml["SourceContainer"]).Remove(sourceViewLoadingVP);
            ((Gtk.ScrolledWindow)gxml["SourceContainer"]).Add(sourceView);
            sourceView.Show();
            
            gxml["SearchLabel"].Sensitive = true;
            searchEntry.Sensitive = true;
        }
        
        private void OnLibraryTransactionStatusStopped(object o, EventArgs args)
        {
            headerNotebook.RemovePage(libraryTransactionStatus);
            headerNotebook.ActivePageWidget = trackInfoHeader;
        }
        
        private void OnHeaderPageCountChanged(object o, EventArgs args)
        {
            HeaderCycleButton.Visible = headerNotebook.Count > 1;
        }
        
        private void OnHeaderCycleButtonClicked(object o, EventArgs args)
        {
            headerNotebook.Cycle();
        }
        
        private void OnLibraryReloaded(object o, EventArgs args)
        {
            LoadSourceView();

            if(LocalQueueSource.Instance.Count > 0) {
                sourceView.SelectSource(LocalQueueSource.Instance);
            } else if(Globals.ArgumentQueue.Contains("audio-cd")) {
                SelectAudioCd(Globals.ArgumentQueue.Dequeue("audio-cd"));
            } else if(Globals.ArgumentQueue.Contains("dap")) {
                SelectDap(Globals.ArgumentQueue.Dequeue("dap"));
            } else {
                sourceView.SelectLibraryForce();
            }

            if(Globals.ArgumentQueue.Contains("play")) {
                GLib.Timeout.Add(1500, delegate {
                    PlayPause();
                    return false;
                });
            }
            
            if(Globals.Library.Tracks.Count <= 0) {
                Application.Invoke(delegate { 
                    PromptForImport();
                });
            }
        }
        
        private bool PromptForImportTimeout()
        {
            LoadSourceView();
            PromptForImport();
            
            return false;
        }
        
        // ---- Misc. Utility Routines ----
      
        public void Quit()
        {
            ActiveUserEventsManager.Instance.CancelAll();
            playlistView.Shutdown();
            PlayerEngineCore.ActivePlayer.Dispose();
            Globals.Configuration.Set(GConfKeys.SourceViewWidth, SourceSplitter.Position);
            DBusRemote.UnregisterObject(banshee_dbus_object);
            PlayerCore.Dispose();
            Globals.Dispose();
            Application.Quit();
        }
      
        private void SetInfoLabel(string text)
        {
            LabelInfo.Markup = "<span size=\"small\">" + GLib.Markup.EscapeText(text) + "</span>";
        }
          
        public void TogglePlaying()
        {
            if(PlayerEngineCore.ActivePlayer.Playing) {
                Globals.ActionManager.UpdateAction("PlayPauseAction", Catalog.GetString("Play"), 
                    "media-playback-start");
                PlayerEngineCore.ActivePlayer.Pause();
            } else {
                Globals.ActionManager.UpdateAction("PlayPauseAction", Catalog.GetString("Pause"), 
                    "media-playback-pause");
                PlayerEngineCore.ActivePlayer.Play();
            }
        }
        
        public void UpdateMetaDisplay(TrackInfo ti)
        {
            trackInfoHeader.Artist = ti.DisplayArtist;
            trackInfoHeader.Title = ti.DisplayTitle;
            trackInfoHeader.Album = ti.DisplayAlbum;
            
            WindowPlayer.Title = ti.DisplayTitle + " - " + Catalog.GetString("Banshee");
            
            try {
                trackInfoHeader.Cover.FileName = ti.CoverArtFileName;
                cover_art_view.FileName = ti.CoverArtFileName;
                trackInfoHeader.Cover.Label = String.Format("{0} - {1}", ti.Artist, ti.Album);
            } catch(Exception) {
            }
            
            if(trayIcon != null) {
                trayIcon.Track = ti;
            }
        }
        
        public void PlayFile(TrackInfo ti)
        {
            PlayerEngineCore.ActivePlayer.Close();
            
            if(ti.Uri == null) {
                return;
            }
            
            if(ti.Uri.Scheme == "cdda") {
                PlayerEngineCore.LoadCdPlayer();
            } else {
                PlayerEngineCore.UnloadCdPlayer();
            }
            
            activeTrackInfo = ti;
            PlayerEngineCore.ActivePlayer.Open(ti, ti.Uri);

            incrementedCurrentSongPlayCount = false;
            ScaleTime.Adjustment.Lower = 0;
            ScaleTime.Adjustment.Upper = ti.Duration.TotalSeconds;

            UpdateMetaDisplay(ti);
            
            TogglePlaying();

            playlistView.QueueDraw();
            
            if(!ti.CanPlay) {
                LogCore.Instance.PushWarning(
                    Catalog.GetString("Cannot Play Song"), 
                    String.Format(Catalog.GetString("{0} cannot be played by Banshee. " +
                        "The most common reasons for this are:\n\n" +
                        "  <big>\u2022</big> Song is protected (DRM)\n" +
                        "  <big>\u2022</big> Song is on a DAP that does not support playback\n"),
                        ti.Title));
            }
        }
        
        // ---- Window Event Handlers ----
        
        private void OnWindowPlayerDeleteEvent(object o, DeleteEventArgs args) 
        {
            Quit();
            args.RetVal = true;
        }
        
        [GLib.ConnectBefore]
        private void OnWindowPlayerConfigureEvent(object o, ConfigureEventArgs args)
        {
            int x, y, width, height;

            if((WindowPlayer.GdkWindow.State & Gdk.WindowState.Maximized) != 0) {
                return;
            }
            
            WindowPlayer.GetPosition(out x, out y);
            WindowPlayer.GetSize(out width, out height);
            
            Globals.Configuration.Set(GConfKeys.WindowX, x);
            Globals.Configuration.Set(GConfKeys.WindowY, y);
            Globals.Configuration.Set(GConfKeys.WindowWidth, width);
            Globals.Configuration.Set(GConfKeys.WindowHeight, height);
        }
        
        [GLib.ConnectBefore]
        private void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            bool handled = false;
            
            switch(args.Event.Key) {
                case Gdk.Key.J:
                case Gdk.Key.j:
                case Gdk.Key.F3:
                    if(!searchEntry.HasFocus) {
                        searchEntry.Focus();
                        handled = true;
                    }
                    break;
                case Gdk.Key.Left:
                    if((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        PlayerEngineCore.ActivePlayer.Position -= SkipDelta;
                        handled = true;
                    } else if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                        PlayerEngineCore.ActivePlayer.Position = 0;
                        handled = true;
                    }
                    break;
                case Gdk.Key.Right:
                    if((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        PlayerEngineCore.ActivePlayer.Position += SkipDelta;
                        handled = true;
                    } 
                    break;
                case Gdk.Key.space:
                    if(!searchEntry.HasFocus && !sourceView.EditingRow) {
                        PlayPause();
                        handled = true;
                    }
                    break;
            }
            
            args.RetVal = handled;
        }
        
        [GLib.ConnectBefore]
        private void OnPlaylistViewKeyPressEvent(object o, KeyPressEventArgs args)
        {
            switch(args.Event.Key) {
                case Gdk.Key.Return:
                    playlistView.PlaySelected();
                    args.RetVal = true;
                    break;
            }
        }       
              
        // ---- Tray Event Handlers ----
      
        private void OnTrayClick(object o, EventArgs args)
        {
            WindowPlayer.Visible = !WindowPlayer.Visible;
            ResizeMoveWindow();
        }
          
        private void OnTrayScroll(object o, ScrollEventArgs args)
        {
            switch(args.Event.Direction) {
                case Gdk.ScrollDirection.Up:
                    if((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {            
                        Volume += VolumeDelta;
                    } else if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                        PlayerEngineCore.ActivePlayer.Position += SkipDelta;
                    } else {
                        Next();
                    }
                    break;
                case Gdk.ScrollDirection.Down:
                    if((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {            
                        Volume -= VolumeDelta;
                    } else if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                        PlayerEngineCore.ActivePlayer.Position -= SkipDelta;
                    } else {
                        Previous();
                    }
                    break;
                default:
                    break;
            }
        }
   
        public int Volume {
            get {
                return volumeButton.Volume;
            }
            
            set {
                if(value != volumeButton.Volume) {
                    volumeButton.Volume = Math.Max(0, Math.Min(100, value));
                    OnVolumeScaleChanged(volumeButton.Volume);
                }
            }
        }
   
        // ---- Playback Event Handlers ----
        
        public void PlayPause()
        {
            if(PlayerEngineCore.ActivePlayer.Loaded) {
                TogglePlaying();
            } else {
                if(!playlistView.PlaySelected()) {
                    playlistModel.Advance();
                    playlistView.UpdateView();
                }
            }
        }
        
        public void Previous()
        {
            playlistModel.Regress();
            playlistView.UpdateView();
        }
        
        public void Next()
        {
            playlistModel.Advance();
            playlistView.UpdateView();
        }
        
        private void OnSpecialKeysPressed(object o, SpecialKey key)
        {
            switch(key) {
                case SpecialKey.AudioPlay:
                    PlayPause();
                    break;
                case SpecialKey.AudioNext:
                    Next();
                    break;
                case SpecialKey.AudioPrev:
                    Previous();
                    break;
            }
        }
        
        private void OnVolumeScaleChanged(int volume)
        {
            PlayerEngineCore.ActivePlayer.Volume = (ushort)volume;
            Globals.Configuration.Set(GConfKeys.Volume, volume);
        }
   
        // ---- Player Event Handlers ----
        
        private void SetPositionLabel(long position)
        {
            if(activeTrackInfo == null) {
                return;
            }
            
            SetInfoLabel(
                // Translators: position in song. eg, "0:37 of 3:48"
                String.Format(Catalog.GetString("{0} of {1}"),
                          String.Format("{0}:{1:00}", position / 60, position % 60),
                          String.Format("{0}:{1:00}", activeTrackInfo.Duration.Minutes, 
                          activeTrackInfo.Duration.Seconds))
            );    
        }
        
        private void OnPlayerTick(object o, PlayerEngineIterateArgs args)
        {
            if(activeTrackInfo == null) {
                return;
            }
             
            if(PlayerEngineCore.ActivePlayer.Length > 0 
                && activeTrackInfo.Duration.TotalSeconds <= 0.0) {
                activeTrackInfo.Duration = new TimeSpan(PlayerEngineCore.ActivePlayer.Length * TimeSpan.TicksPerSecond);
                activeTrackInfo.Save();
                playlistView.ThreadedQueueDraw();
                ScaleTime.Adjustment.Upper = activeTrackInfo.Duration.TotalSeconds;
            }
                
            if(PlayerEngineCore.ActivePlayer.Length > 0 && 
                PlayerEngineCore.ActivePlayer.Position > PlayerEngineCore.ActivePlayer.Length / 2
                && !incrementedCurrentSongPlayCount) {
                activeTrackInfo.IncrementPlayCount();
                incrementedCurrentSongPlayCount = true;
                playlistView.ThreadedQueueDraw();
            }
                
            if(updateEnginePosition) {
                if(setPositionTimeoutId > 0)
                        GLib.Source.Remove(setPositionTimeoutId);
                setPositionTimeoutId = GLib.Timeout.Add(100,
                        new GLib.TimeoutHandler(SetPositionTimeoutCallback));
            
                Application.Invoke(delegate {
                    SetPositionLabel(args.Position);
                    if(PlayerEngineCore.ActivePlayer.Playing) {
                        trayIcon.Update();
                    }
                });
            }
        }
        
        private bool SetPositionTimeoutCallback()
        {
            setPositionTimeoutId = 0;
            Application.Invoke(delegate {
                ScaleTime.Value = PlayerEngineCore.ActivePlayer.Position;
            });
            
            return false;
        }
        
        [GLib.ConnectBeforeAttribute]
        private void OnScaleTimeMoveSlider(object o, EventArgs args)
        {
            SetPositionLabel((long)ScaleTime.Value);
        }
        
        [GLib.ConnectBeforeAttribute]
        private void OnScaleTimeButtonPressEvent(object o, 
            ButtonPressEventArgs args)
        {
            updateEnginePosition = false;
        }
        
        [GLib.ConnectBeforeAttribute]
        private void OnScaleTimeButtonReleaseEvent(object o, 
            ButtonReleaseEventArgs args)
        {
            PlayerEngineCore.ActivePlayer.Position = (uint)ScaleTime.Value;
            updateEnginePosition = true;
        }
        
        private void OnPlayerEos(object o, EventArgs args)
        {
            Application.Invoke(delegate {
                StopPlaying();
                playlistModel.Continue();
                playlistView.UpdateView();
            
            });
        }
        
        // ---- Playlist Event Handlers ----

        private void ImportWithFileSelector()
        {
            FileChooserDialog chooser = new FileChooserDialog(
                Catalog.GetString("Import Folder to Library"),
                null,
                FileChooserAction.SelectFolder
            );
            
            try {
                 chooser.SetCurrentFolderUri(Globals.Configuration.Get(GConfKeys.LastFileSelectorUri) as string);
            } catch(Exception) {
                 chooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            }
            
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;
            
            if(chooser.Run() == (int)ResponseType.Ok) { 
                ImportManager.Instance.QueueSource(chooser.Uri);
            }
            
            Globals.Configuration.Set(GConfKeys.LastFileSelectorUri, chooser.CurrentFolderUri);
            
            chooser.Destroy();
        }

        private void OnLoaderHaveTrackInfo(object o, HaveTrackInfoArgs args)
        {
            sourceView.QueueDraw();
            playlistView.QueueDraw();
        }
        
        private void ImportHomeDirectory()
        {
            ImportManager.Instance.QueueSource(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        }
        
        private void OnPlaylistSaved(object o, PlaylistSavedArgs args)
        {    
            sourceView.AddPlaylist(args.Name);
        }
        
        private void OnPlaylistObjectUpdated(object o, EventArgs args)
        {
            sourceView.ThreadedQueueDraw();
        }

        private void OnPlaylistViewSelectionChanged(object o, EventArgs args)
        {
            int count = playlistView.Selection.CountSelectedRows();
            bool have_selection = count > 0;
            
            if(!have_selection) {
                Globals.ActionManager.SongActions.Sensitive = false;
                return;
            }
            
            Source source = sourceView.SelectedSource;

            if(source == null) {
                return;
            }
            
            Globals.ActionManager.SongActions.Sensitive = true;
            Globals.ActionManager["WriteCDAction"].Sensitive = !(source is AudioCdSource);
            Globals.ActionManager["RemoveSongsAction"].Sensitive = !(source is AudioCdSource);
            Globals.ActionManager["DeleteSongsFromDriveAction"].Sensitive = 
                !(source is AudioCdSource || source is DapSource);
        }
        
        private void OnSourceChanged(object o, EventArgs args)
        {
            ThreadAssist.ProxyToMain(HandleSourceChanged);
        }
        
        private void SensitizeActions(Source source)
        {
            Globals.ActionManager["WriteCDAction"].Visible = !(source is AudioCdSource);
            Globals.ActionManager.AudioCdActions.Visible = source is AudioCdSource;
            Globals.ActionManager["RenameSourceAction"].Sensitive = source.CanRename;
            Globals.ActionManager.PlaylistActions.Sensitive = source is PlaylistSource;
            Globals.ActionManager.SourceEjectActions.Visible = source.CanEject;
            Globals.ActionManager.DapActions.Visible = source is DapSource;
            Globals.ActionManager["SelectedSourcePropertiesAction"].Sensitive = source.HasProperties;
            
            if(source is DapSource) {
                DapSource dapSource = source as DapSource;
                Globals.ActionManager["SyncDapAction"].Sensitive = !dapSource.Device.IsReadOnly;
                Globals.ActionManager.SetActionLabel("SyncDapAction", String.Format("{0} {1}",
                    Catalog.GetString("Synchronize"), dapSource.Device.GenericName));
                Globals.ActionManager["RenameSourceAction"].Label = Catalog.GetString("Rename Device");
            } else {
                Globals.ActionManager["RenameSourceAction"].Label = Catalog.GetString("Rename Playlist");            
            }
        }
        
        private void OnAudioCdStatusBarButtonClicked(object o, EventArgs args)
        {
            AudioCdSource source = sourceView.SelectedSource as AudioCdSource;
            if(source == null) {
                return;
            }
            
            source.Disk.QueryMetadata();
        }
        
        private void UpdateAudioCdStatus(AudioCdDisk disk)
        {
            string status = null;
            Gdk.Pixbuf icon = null;
            
            switch(disk.Status) {
                case AudioCdLookupStatus.ReadingDisk:
                    status = Catalog.GetString("Reading table of contents from CD...");
                    icon = IconThemeUtils.LoadIcon(22, "media-cdrom", "gnome-dev-cdrom-audio", "source-cd-audio");
                    audiocd_statusbar.ShowCloseButton = false;
                    break;
                case AudioCdLookupStatus.SearchingMetadata:
                    status = Catalog.GetString("Searching for CD metadata...");
                    icon = IconThemeUtils.LoadIcon(22, "system-search", Stock.Find);
                    audiocd_statusbar.ShowCloseButton = false;
                    break;
                case AudioCdLookupStatus.SearchingCoverArt:
                    status = Catalog.GetString("Searching for CD cover art...");
                    icon = IconThemeUtils.LoadIcon(22, "system-search", Stock.Find);
                    audiocd_statusbar.ShowCloseButton = false;
                    break;
                case AudioCdLookupStatus.ErrorNoConnection:
                    status = Catalog.GetString("Cannot search for CD metadata: " + 
                        "there is no available Internet connection");
                    icon = IconThemeUtils.LoadIcon(22, "network-wired", Stock.Network);
                    audiocd_statusbar.ShowCloseButton = true;
                    break;
                case AudioCdLookupStatus.ErrorLookup:
                    status = Catalog.GetString("Could not fetch metadata for CD.");
                    icon = IconThemeUtils.LoadIcon(22, Stock.DialogError);
                    audiocd_statusbar.ShowCloseButton = true;
                    break;
                case AudioCdLookupStatus.Success:
                default:
                    status = null;
                    icon = null;
                    break;
            }
            
            if(disk.Status == AudioCdLookupStatus.ErrorLookup) {
                audiocd_statusbar.ButtonLabel = Stock.Refresh;
                audiocd_statusbar.ButtonUseStock = true;
            } else {
                audiocd_statusbar.ButtonLabel = null;
            }
            
            audiocd_statusbar.Visible = status != null;
            audiocd_statusbar.Message = String.Format("<big>{0}</big>", GLib.Markup.EscapeText(status));
            audiocd_statusbar.Pixbuf = icon;
        }
        
        private void HandleSourceChanged(object o, EventArgs args)
        {
            Source source = sourceView.SelectedSource;
            if(source == null) {
                return;
            }

            searchEntry.CancelSearch(false);
            audiocd_statusbar.Visible = false;
            
            if(source is LibrarySource) {
                playlistModel.LoadFromLibrary();
                playlistModel.Source = source;
            } else if(source is LocalQueueSource) {
                playlistModel.LoadFromLocalQueue();
                playlistModel.Source = source;
            } else if(source is DapSource) {
                playlistModel.Clear();
                playlistModel.Source = source;
                DapSource dap_source = source as DapSource;
                playlistModel.LoadFromDapSource(dap_source);
                UpdateDapDiskUsageBar(dap_source);
            } else if(source is AudioCdSource) {
                playlistModel.Clear();
                playlistModel.Source = source;
                AudioCdSource cdSource = source as AudioCdSource;
                playlistModel.LoadFromAudioCdSource(cdSource);
                UpdateAudioCdStatus(cdSource.Disk);
            } else {
                playlistModel.LoadFromPlaylist(source.Name);
                playlistModel.Source = source;
            }
            
            (gxml["ViewNameLabel"] as Label).Markup = "<b>" + GLib.Markup.EscapeText(source.Name) + "</b>";

            SensitizeActions(source);

            if(source is DapSource) {
                gxml["DapContainer"].ShowAll();
                sync_dap_button.Pixbuf = (source as DapSource).Device.GetIcon(22);
            } else {
                gxml["DapContainer"].Hide();
            }
            
            gxml["SearchLabel"].Sensitive = (source is DapSource && !((source as DapSource).IsSyncing)) 
                || source is LibrarySource;
            searchEntry.Sensitive = gxml["SearchLabel"].Sensitive;
            playlistView.RipColumn.Visible = source is AudioCdSource;
            playlistView.RatingColumn.Visible = !(source is AudioCdSource);
            playlistView.PlaysColumn.Visible = playlistView.RatingColumn.Visible;
            playlistView.LastPlayedColumn.Visible = playlistView.RatingColumn.Visible;
                
            if(source.Type != SourceType.Dap) {
                ShowPlaylistView();
            } else if((source as DapSource).IsSyncing) {
                ShowSyncingView();
            } else {
                ShowPlaylistView();
            }
                
            OnPlaylistViewSelectionChanged(playlistView.Selection, new EventArgs());
        }
        
        private void UpdateDapDiskUsageBar(DapSource dapSource)
        {
            Application.Invoke(delegate {
                dapDiskUsageBar.Fraction = dapSource.DiskUsageFraction;
                dapDiskUsageBar.Text = dapSource.DiskUsageString;
                string tooltip = dapSource.DiskUsageString + " (" + dapSource.DiskAvailableString + ")";
                toolTips.SetTip(dapDiskUsageBar, tooltip, tooltip);
            });
        }

        private void OnDapPropertiesChanged(object o, EventArgs args)
        {
            Application.Invoke(delegate {
                DapDevice device = o as DapDevice;
                
                foreach(object [] obj in (sourceView.Model as ListStore)) {
                    if(obj[0] is DapSource && (obj[0] as DapSource).Device == device) {
                        (obj[0] as DapSource).SetSourceName(device.Name);
                        sourceView.QueueDraw();
                    }
                }
                
                if(playlistModel.Source is DapSource && (playlistModel.Source as DapSource).Device == device) {
                    UpdateDapDiskUsageBar(playlistModel.Source as DapSource);
                    (gxml["ViewNameLabel"] as Label).Markup = "<b>" 
                        + GLib.Markup.EscapeText(device.Name) + "</b>";
                    sourceView.QueueDraw();
                }
            });
        }
        
        private void OnDapCoreDeviceAdded(object o, DapEventArgs args)
        {
            args.Dap.PropertiesChanged += OnDapPropertiesChanged;
            args.Dap.SaveStarted += OnDapSaveStarted;
            args.Dap.SaveFinished += OnDapSaveFinished;
        }
        
        private void OnAudioCdCoreDiskRemoved(object o, AudioCdCoreDiskRemovedArgs args)
        {
            Source source = sourceView.SelectedSource;
            if(source == null) {
                return;
            }

            if(source.Type == SourceType.AudioCd) {
                AudioCdSource cdSource = source as AudioCdSource;
                if(cdSource.Disk.Udi == args.Udi) {
                    sourceView.SelectLibrary();
                }
            }
        }

        private void OnAudioCdCoreUpdated(object o, EventArgs args)
        {
            Source source = sourceView.SelectedSource;
            if(source == null) {
                return;
            }
            
            playlistView.QueueDraw();
       
            if(source.Type == SourceType.AudioCd && playlistModel.FirstTrack != null 
                && playlistModel.FirstTrack.GetType() == typeof(AudioCdTrackInfo)) {
                AudioCdSource cdSource = source as AudioCdSource;
                AudioCdTrackInfo track = playlistModel.FirstTrack as AudioCdTrackInfo;
               
                if(cdSource.Disk.DeviceNode == track.Device) { 
                    (gxml["ViewNameLabel"] as Label).Markup = "<b>" + 
                        GLib.Markup.EscapeText(cdSource.Disk.Title) + "</b>";
                    UpdateAudioCdStatus(cdSource.Disk);
                }
            }
        }
        
        private void OnDapSaveStarted(object o, EventArgs args)
        {
            if(playlistModel.Source.Type == SourceType.Dap 
                && (playlistModel.Source as DapSource).IsSyncing) {
                ShowSyncingView();
                gxml["SearchLabel"].Sensitive = false;
                searchEntry.Sensitive = false;
                Globals.ActionManager.DapActions.Sensitive = false;
                dap_syncing_image.Pixbuf = (playlistModel.Source as DapSource).Device.GetIcon(128);
            }
        }
        
        private void OnDapSaveFinished(object o, EventArgs args)
        {
            if(playlistModel.Source.Type == SourceType.Dap 
                && !(playlistModel.Source as DapSource).IsSyncing) {
                ShowPlaylistView();
                gxml["SearchLabel"].Sensitive = true;
                searchEntry.Sensitive = true;
                Globals.ActionManager.DapActions.Sensitive = true;
                playlistModel.LoadFromDapSource(playlistModel.Source as DapSource);
                UpdateDapDiskUsageBar(playlistModel.Source as DapSource);
            }
            
            sourceView.QueueDraw();
            playlistView.QueueDraw();
        }

        private void ShowPlaylistView()
        {
            Alignment alignment = gxml["LibraryAlignment"] as Alignment;
            ScrolledWindow playlist_container = gxml["LibraryContainer"] as ScrolledWindow;
            
            if(alignment.Child == playlist_container) {
                return;
            } else if(alignment.Child == syncing_container) {
                alignment.Remove(syncing_container);
            }
            
            alignment.Add(playlist_container);
            alignment.ShowAll();
        }
        
        private void ShowSyncingView()
        {
            Alignment alignment = gxml["LibraryAlignment"] as Alignment;
            ScrolledWindow playlist_container = gxml["LibraryContainer"] as ScrolledWindow;
            
            if(alignment.Child == syncing_container) {
                return;
            } else if(alignment.Child == playlist_container) {
                alignment.Remove(playlist_container);
            }
            
            if(syncing_container == null) {
                syncing_container = new EventBox();
                HBox syncing_box = new HBox();
                syncing_container.Add(syncing_box);
                syncing_box.Spacing = 20;
                syncing_box.PackStart(dap_syncing_image, false, false, 0);
                Label syncing_label = new Label();
                                                
                syncing_container.ModifyBg(StateType.Normal, new Color(0, 0, 0));
                syncing_label.ModifyFg(StateType.Normal, new Color(160, 160, 160));
            
                syncing_label.Markup = "<big><b>" + GLib.Markup.EscapeText(
                    Catalog.GetString("Synchronizing your Device, Please Wait...")) + "</b></big>";
                syncing_box.PackStart(syncing_label, false, false, 0);
            }
            
            alignment.Add(syncing_container);
            alignment.ShowAll();
        }

        private void OnPlaylistUpdated(object o, EventArgs args)
        {
            long count = playlistModel.Count();
            TimeSpan span = playlistModel.TotalDuration;       
            string timeDisp = String.Empty;
            
            if(span.Days > 0) {
                timeDisp = String.Format(Catalog.GetPluralString("{0} day", "{0} days", span.Days), 
                    span.Days) + " ";
            }
            
            if(span.Days > 0 || span.Hours > 0) {
                timeDisp += String.Format("{0}:{1}:{2}", span.Hours, span.Minutes.ToString("00"), 
                    span.Seconds.ToString("00"));
            } else {   
                timeDisp += String.Format("{0}:{1}", span.Minutes, span.Seconds.ToString("00"));
            }
            
            ThreadAssist.ProxyToMain(delegate {
                if(count == 0 && playlistModel.Source == null) {
                    LabelStatusBar.Text = Catalog.GetString("Banshee Music Player");
                } else if(count == 0) {
                    switch(playlistModel.Source.Type) {
                        case SourceType.Library:
                            LabelStatusBar.Text = Catalog.GetString(
                                "Your Library is Empty - Consider Importing Music");
                            break;
                        case SourceType.Playlist:
                            LabelStatusBar.Text = Catalog.GetString(
                                "This Playlist is Empty - Consider Adding Music");
                            break;
                    }
                } else {
                    string text = String.Format(Catalog.GetPluralString("{0} Item", "{0} Items", 
                        (int)count), count) + ", ";
                    text += String.Format(Catalog.GetString("{0} Total Play Time"), timeDisp);
                    LabelStatusBar.Text = text;
                }
            });
        }
        
        private void OnLogCoreUpdated(object o, LogCoreUpdatedArgs args)
        {
            if(!args.Entry.ShowUser || args.Entry.Type == LogEntryType.Debug) {
                return;
            }
            
            MessageType mtype;
            
            switch(args.Entry.Type) {
                case LogEntryType.Warning:
                    mtype = MessageType.Warning;
                    break;
                case LogEntryType.Error:
                default:
                    mtype = MessageType.Error;
                    break;
            }
              
            HigMessageDialog dialog = new HigMessageDialog(WindowPlayer, 
                DialogFlags.Modal,
                mtype,
                ButtonsType.Ok,
                args.Entry.ShortMessage,
                args.Entry.Details);
            
            dialog.Title = args.Entry.ShortMessage;
            IconThemeUtils.SetWindowIcon(dialog);
            
            dialog.Response += delegate(object o, ResponseArgs args)
            {
                (o as Dialog).Destroy();
            };
            
            dialog.ShowAll();
        }
     
        private uint popupTime;
        private Menu source_menu = null;
        
        [GLib.ConnectBefore]
        private void OnSourceViewButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if(args.Event.Button != 3) {
                return;
            }
                
            TreePath path;
            if(!sourceView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path)) {
                args.RetVal = true; 
                return;
            }
            
            sourceView.HighlightPath(path);
            Source source = sourceView.GetSource(path);
            
            if(source is LibrarySource) {
                args.RetVal = true;
                return;
            }

            SensitizeActions(source);
            
            if(source_menu == null) {
                source_menu = Globals.ActionManager.GetWidget("/SourceMenu") as Menu;
                source_menu.SelectionDone += delegate(object o, EventArgs args) {
                    SensitizeActions(sourceView.SelectedSource);
                    sourceView.ResetHighlight();
                };
            }
            
            source_menu.Popup(null, null, null, 0, args.Event.Time);
            source_menu.Show();
            
            args.RetVal = true;
        }
      
        private bool DoesTrackMatchSearch(TrackInfo ti)
        {
            if(!searchEntry.IsQueryAvailable) {
                return false;
            }
            
            string query = searchEntry.Query.ToLower();
            string field = searchEntry.Field;
            string match = null;
            
            if(field == Catalog.GetString("Artist Name")) {
                match = ti.Artist;
            } else if(field == Catalog.GetString("Song Name")) {
                match = ti.Title;
            } else if(field == Catalog.GetString("Album Title")) {
                match = ti.Album;
            } else {
                string [] matches = {
                    ti.Artist,
                    ti.Album,
                    ti.Title
                };

                foreach(string m in matches) {
                    if(m == null || m == String.Empty) {
                        continue;
                    }

                    string ml = m.ToLower();
                    if(ml.IndexOf(query) >= 0 || ml.IndexOf("the " + query) >= 0) {
                        return true;
                    }
                }
                
                return false;
            }
                    
            match = match.ToLower();
            return match.IndexOf(query) >= 0 || match.IndexOf("the " + query) >= 0;
        }
        
        private void OnSimpleSearch(object o, EventArgs args)
        {
            Source source = sourceView.SelectedSource;
            playlistModel.Clear();
            
            if(!searchEntry.IsQueryAvailable) {
                if(source.Type == SourceType.Dap) {
                    playlistModel.LoadFromDapSource(source as DapSource);
                } else {
                    playlistModel.LoadFromLibrary();
                }
                
                return;
            }
            
            ICollection collection = null;
            
            if(source.Type == SourceType.Dap) {
                collection = (source as DapSource).Device.Tracks;
            } else {
                collection = Globals.Library.Tracks.Values;
            }
            
            foreach(TrackInfo ti in collection) {
                try {
                    if(DoesTrackMatchSearch(ti)) {
                        playlistModel.AddTrack(ti);
                    }
                } catch(Exception) {
                    continue;
                }
            }
        }
        
        // PlaylistView DnD
        
        [GLib.ConnectBefore]
        private void OnPlaylistViewButtonPressEvent(object o, 
            ButtonPressEventArgs args)
        {
            if (args.Event.Window != playlistView.BinWindow)
                return;

            if(args.Event.Button == 3) {
                //GLib.Timeout.Add(10, 
                //    new GLib.TimeoutHandler(PlaylistMenuPopupTimeout));
                PlaylistMenuPopupTimeout(args.Event.Time);
            }
            
            TreePath path;
            playlistView.GetPathAtPos((int)args.Event.X, 
                (int)args.Event.Y, out path);
        
            if(path == null)
                return;
            
            clickX = (int)args.Event.X;
            clickY = (int)args.Event.Y;
        
            switch(args.Event.Type) {
                case EventType.TwoButtonPress:
                    if(args.Event.Button != 1
                        || (args.Event.State &  (ModifierType.ControlMask 
                        | ModifierType.ShiftMask)) != 0)
                        return;
                    playlistView.Selection.UnselectAll();
                    playlistView.Selection.SelectPath(path);
                    playlistView.PlayPath(path);
                    return;
                case EventType.ButtonPress:
                    if(playlistView.Selection.PathIsSelected(path) &&
                   (args.Event.State & (ModifierType.ControlMask |
                            ModifierType.ShiftMask)) == 0)
                        args.RetVal = true;
                    return;
                default:
                    args.RetVal = false;
                    return;
            }
        }

        [GLib.ConnectBefore]
        private void OnPlaylistViewMotionNotifyEvent(object o, 
            MotionNotifyEventArgs args)
        {
            if((args.Event.State & ModifierType.Button1Mask) == 0)
                return;
            if(args.Event.Window != playlistView.BinWindow)
                return;
                    
            args.RetVal = true;
            if(!Gtk.Drag.CheckThreshold(playlistView, clickX, clickY,
                            (int)args.Event.X, (int)args.Event.Y))
                return;
            TreePath path;
            if (!playlistView.GetPathAtPos((int)args.Event.X, 
                               (int)args.Event.Y, out path))
                return;

           if(sourceView.SelectedSource.Type == SourceType.AudioCd)
              return;
              
            Gtk.Drag.Begin(playlistView, new TargetList (playlistViewSourceEntries),
                       Gdk.DragAction.Move | Gdk.DragAction.Copy, 1, args.Event);
        }

        private void OnPlaylistViewButtonReleaseEvent(object o, 
            ButtonReleaseEventArgs args)
        {
            if(!Gtk.Drag.CheckThreshold(playlistView, clickX, clickY,
                            (int)args.Event.X, (int)args.Event.Y) &&
               ((args.Event.State & (ModifierType.ControlMask 
                         | ModifierType.ShiftMask)) == 0) &&
               playlistView.Selection.CountSelectedRows() > 1) {
                TreePath path;
                playlistView.GetPathAtPos((int)args.Event.X, 
                              (int)args.Event.Y, out path);
                playlistView.Selection.UnselectAll();
                playlistView.Selection.SelectPath(path);
            }
        }
        
        private void OnNewPlaylistFromSelectionActivated(object o, EventArgs args)
        {
            ArrayList tracks = new ArrayList();
            
            foreach(TreePath path in playlistView.Selection.GetSelectedRows()) {
                TrackInfo ti = playlistModel.PathTrackInfo(path);
                tracks.Add(ti);
            }
            
            Playlist pl = new Playlist(Playlist.GoodUniqueName(tracks));
            pl.Append(tracks);
            pl.Save();
            pl.Saved += OnPlaylistSaved;
        }
        
        private void OnItemAddToPlaylistActivated(object o, EventArgs args)
        {
            string name = playlistMenuMap[o] as string;
            
            if(name == null)
                return;
                
            playlistView.AddSelectedToPlayList(name);
        }

        private Menu song_popup_menu = null;
        private MenuItem add_to_playlist_menu_item = null;
        private MenuItem rating_menu_item = null;
        
        private bool PlaylistMenuPopupTimeout(uint time)
        {
            if(song_popup_menu == null) {
                song_popup_menu = Globals.ActionManager.GetWidget("/SongViewPopup") as Menu;
                add_to_playlist_menu_item = Globals.ActionManager.GetWidget(
                    "/SongViewPopup/AddToPlaylist") as MenuItem;
                rating_menu_item = Globals.ActionManager.GetWidget("/SongViewPopup/Rating") as MenuItem;
            }
          
            bool sensitive = playlistView.Selection.CountSelectedRows() > 0;

            if(sensitive && (playlistModel.Source is LibrarySource || playlistModel.Source is PlaylistSource)) {
                Globals.ActionManager["AddToPlaylistAction"].Visible = true;
                Globals.ActionManager["RatingAction"].Visible = true;
            
                Menu plMenu = new Menu();
                playlistMenuMap.Clear();
                
                ImageMenuItem newPlItem = new ImageMenuItem(Catalog.GetString("New Playlist"));
                newPlItem.Image = new Gtk.Image("gtk-new", IconSize.Menu);
                newPlItem.Activated += OnNewPlaylistFromSelectionActivated;
                plMenu.Append(newPlItem);
                
                string [] names = Playlist.ListAll();
                
                if(names.Length > 0) {
                    plMenu.Append(new SeparatorMenuItem());
                    
                    foreach(string plName in names) {
                        ImageMenuItem item = new ImageMenuItem(plName);
                        item.Image = new Gtk.Image(Pixbuf.LoadFromResource("source-playlist.png"));
                        
                        playlistMenuMap[item] = plName;
                        item.Activated += OnItemAddToPlaylistActivated;
                        
                        plMenu.Append(item);
                    }
                }
                
                Menu ratingMenu = new Menu();
                
                MenuItem clearItem = new MenuItem(Catalog.GetString("Clear"));
                clearItem.Name = "0";
                clearItem.Activated += OnItemRatingActivated;
                
                ratingMenu.Append(clearItem);
                ratingMenu.Append(new SeparatorMenuItem());
                
                for(int i = 0; i < 5; i++) {
                    MenuItem item = new MenuItem();
                    HBox box = new HBox();
                    box.Spacing = 3;
                    
                    for(int j = 0; j < i + 1; j++) {
                        box.PackStart(new Gtk.Image(RatingRenderer.Star), false, false, 0);
                    }
                    
                    item.Add(box);
                    item.Name = String.Format("{0}", i + 1);
                    item.Activated += OnItemRatingActivated;
                    ratingMenu.Append(item);
                }
                
                add_to_playlist_menu_item.Submenu = plMenu;
                rating_menu_item.Submenu = ratingMenu;
                
                plMenu.ShowAll();
                ratingMenu.ShowAll();
            } else {
                Globals.ActionManager["AddToPlaylistAction"].Visible = false;
                Globals.ActionManager["RatingAction"].Visible = false;
            }
        
            song_popup_menu.ShowAll();
            song_popup_menu.Popup(null, null, null, 0, time);
            
            return false;
        }

        private void OnItemRatingActivated(object o, EventArgs args)
        {
            uint rating = Convert.ToUInt32((o as Widget).Name);
            foreach(TreePath path in playlistView.Selection.GetSelectedRows())
                playlistModel.PathTrackInfo(path).Rating = rating;
            playlistView.QueueDraw();
        }

        private void OnPlaylistViewDragDataReceived(object o, 
            DragDataReceivedArgs args)
        {
            TreePath destPath;
            TreeIter destIter;
            TreeViewDropPosition pos;
            bool haveDropPosition;
            
            string rawSelectionData = 
                Dnd.SelectionDataToString(args.SelectionData);            
            
            haveDropPosition = playlistView.GetDestRowAtPos(args.X, 
                args.Y, out destPath, out pos);
            
            if(haveDropPosition && 
                !playlistModel.GetIter(out destIter, destPath)) {
                Gtk.Drag.Finish(args.Context, true, false, args.Time);
                return;
            }

            switch(args.Info) {
                case (uint)Dnd.TargetType.UriList:
                    // AddFile needs to accept a Path for inserting
                    // If in Library view, we just append to Library
                    // If in Playlist view, we append Library *AND* PlayList
                    // If in SmartPlaylist View WE DO NOT ACCEPT DND
                
                    if(rawSelectionData != null) {
                        ImportManager.Instance.QueueSource(args.SelectionData);
                    }
                        
                    break;
                case (uint)Dnd.TargetType.PlaylistRows:
                    if(!haveDropPosition)
                        break;
                    
                    string [] paths = Dnd.SplitSelectionData(rawSelectionData);
                    if(paths.Length <= 0) 
                        break;
                        
                    ArrayList iters = new ArrayList();
                    foreach(string path in paths) {
                        if(path == null || path.Length == 0)
                            continue;
                        
                        TreeIter iter;
                        if(!playlistModel.GetIter(out iter, new TreePath(path)))
                            continue;
                            
                        iters.Add(iter);
                    }
                        
                    foreach(TreeIter iter in iters) {
                        if(!playlistModel.IterIsValid(destIter))
                            break;

                        if (pos == TreeViewDropPosition.After ||
                            pos == TreeViewDropPosition.IntoOrAfter) {
                            playlistModel.MoveAfter(iter, destIter);
                            //destIter = iter.Copy();
                            destIter = (TreeIter)iter;
                        } else
                            playlistModel.MoveBefore(iter, destIter);
                    }
                                    
                    break;
            }

            Gtk.Drag.Finish(args.Context, true, false, args.Time);
        }
        
        private void OnPlaylistViewDragDataGet(object o, DragDataGetArgs args)
        {
            byte [] selData;
            
            switch(args.Info) {
                case (uint)Dnd.TargetType.PlaylistRows:                
                    selData = Dnd.TreeViewSelectionPathsToBytes(playlistView);
                    if(selData == null)
                        return;
                    
                    args.SelectionData.Set(
                        Gdk.Atom.Intern(Dnd.TargetPlaylistRows.Target, 
                        false), 8, selData);
                        
                    break;
                case (uint)Dnd.TargetType.LibraryTrackIds:
                    selData = Dnd.PlaylistSelectionTrackIdsToBytes(playlistView);
                    if(selData == null)
                        return;
                    
                    args.SelectionData.Set(
                        Gdk.Atom.Intern(Dnd.TargetLibraryTrackIds.Target,
                        false), 8, selData);
                        
                    break;
                case (uint)Dnd.TargetType.UriList:
                    selData = Dnd.PlaylistViewSelectionUrisToBytes(playlistView);
                    if(selData == null)
                        return;
            
                    args.SelectionData.Set(args.Context.Targets[0],
                        8, selData, selData.Length);
                        
                    break;
            }
        }
        
        private void OnPlaylistViewDragDrop(object o, DragDropArgs args)
        {
            Gtk.Drag.Finish(args.Context, true, false, args.Time);
            
            // major weird hack
            TreePath [] selrows = playlistView.Selection.GetSelectedRows();
            playlistView.Selection.UnselectAll();
            playlistView.Selection.SelectPath(new TreePath("0"));
            playlistView.Selection.UnselectAll();
            foreach(TreePath path in selrows)
                playlistView.Selection.SelectPath(path);
        }
     
        private void OnRipTransactionTrackRipped(object o, HaveTrackInfoArgs args)
        {
            if(playlistModel.Source is LibrarySource) {
                ThreadAssist.ProxyToMain(delegate {
                    if(searchEntry.IsQueryAvailable && !DoesTrackMatchSearch(args.TrackInfo)) {
                        return;
                    }

                    playlistModel.AddTrack(args.TrackInfo);
                });
            }
        }
        
        private void EjectSource(Source source)
        {
            if(source.CanEject) {
                try {
                    if(source.GetType() == typeof(DapSource)) {
                        if(activeTrackInfo != null && activeTrackInfo is DapTrackInfo) {
                            StopPlaying();
                        }
                    }
                        
                    source.Eject();
                    
                    if(source == sourceView.SelectedSource) {
                        sourceView.SelectLibrary();
                    }
                } catch(Exception e) {
                    HigMessageDialog.RunHigMessageDialog(null, 
                        DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, 
                        Catalog.GetString("Could Not Eject"),
                        e.Message);
                }
            }
        }
        
        private void StopPlaying()
        {
            PlayerEngineCore.ActivePlayer.Close();
            Globals.ActionManager.UpdateAction("PlayPauseAction", Catalog.GetString("Play"), "media-playback-start");
            ScaleTime.Adjustment.Lower = 0;
            ScaleTime.Adjustment.Upper = 0;
            ScaleTime.Value = 0;
            SetInfoLabel(Catalog.GetString("Idle"));
            trackInfoHeader.SetIdle();
            activeTrackInfo = null;
            
            if(trayIcon != null) {
                trayIcon.Track = null;
            }
        }

        public TrackInfo ActiveTrackInfo
        {
            get {
                return activeTrackInfo;
            }
        }
        
        private void OnImportManagerImportRequested(object o, ImportEventArgs args)
        {
            try {
                TrackInfo ti = new LibraryTrackInfo(args.FileName);
                args.ReturnMessage = String.Format("{0} - {1}", ti.Artist, ti.Title);
                if(playlistModel.Source is LibrarySource) {
                    ThreadAssist.ProxyToMain(delegate {
                        if(searchEntry.IsQueryAvailable && !DoesTrackMatchSearch(ti)) {
                            return;
                        }
                        
                        playlistModel.AddTrack(ti);
                    });
                }
            } catch(Entagged.Audioformats.Exceptions.CannotReadException) {
                Console.WriteLine(Catalog.GetString("Cannot Import") + ": {0}", args.FileName);
                args.ReturnMessage = Catalog.GetString("Scanning") + "...";
            } catch(Exception e) {
                Console.WriteLine(Catalog.GetString("Cannot Import: {0} ({1}, {2})"), 
                    args.FileName, e.GetType(), e.Message);
                args.ReturnMessage = Catalog.GetString("Scanning") + "...";
            }
        }
        
        private void RemoveSongs(bool deleteFromFileSystem)
        {
            // Don't steal "Del" key from the search entry
            if (WindowPlayer.Focus is Entry &&
                Gtk.Global.CurrentEvent is Gdk.EventKey) {
                Gtk.Bindings.ActivateEvent(WindowPlayer.Focus, 
                    (Gdk.EventKey)Gtk.Global.CurrentEvent);
                return;
            }

            int selCount = playlistView.Selection.CountSelectedRows();
        
            if(selCount <= 0) {
                return;
            }
            
            if(playlistModel.Source.Type == SourceType.Library) {
                string msg = String.Empty;
                
                if(deleteFromFileSystem) {
                    msg = String.Format(
                    Catalog.GetPluralString(
                        "Are you sure you want to remove the selected song from your library <i><b>and</b></i> " +
                        "your drive? This action will permanently delete the file.",
                        "Are you sure you want to remove the selected <b>({0})</b> songs from your library " + 
                        "<i><b>and</b></i> your drive? This action will permanently delete the files.",
                        selCount),
                    selCount);
                } else {
                    msg = String.Format(
                    Catalog.GetPluralString(
                        "Are you sure you want to remove the selected song from your library?",
                        "Are you sure you want to remove the selected <b>({0})</b> songs from your library?",
                        selCount),
                    selCount);
                }
                    
                HigMessageDialog md = new HigMessageDialog(WindowPlayer, 
                    DialogFlags.DestroyWithParent, MessageType.Warning,
                    ButtonsType.YesNo,
                    Catalog.GetString("Remove Selected Songs from Library"),
                    msg);
                if(md.Run() != (int)ResponseType.Yes) {
                    md.Destroy();
                    return;
                }
        
                md.Destroy();
            }
        
            TreeIter [] iters = new TreeIter[selCount];
            int i = 0;
            
            foreach(TreePath path in playlistView.Selection.GetSelectedRows()) {
                playlistModel.GetIter(out iters[i++], path);
            }
            
            TrackRemoveTransaction transaction;
            
            if(playlistModel.Source.Type == SourceType.Dap) {
                for(i = 0; i < iters.Length; i++) {
                    TrackInfo ti = playlistModel.IterTrackInfo(iters[i]);
                    playlistModel.RemoveTrack(ref iters[i]);
                    
                    DapTrackInfo iti = ti as DapTrackInfo;
                    (playlistModel.Source as DapSource).Device.RemoveTrack(iti);
                }
                sourceView.QueueDraw();
                return;
            } else if(playlistModel.Source.Type == SourceType.Library) {
                transaction = new LibraryTrackRemoveTransaction();
            } else {
                transaction = new PlaylistTrackRemoveTransaction(
                    Playlist.GetId(playlistModel.Source.Name));
            }
              
            for(i = 0; i < iters.Length; i++) {
                TrackInfo ti = playlistModel.IterTrackInfo(iters[i]);
                playlistModel.RemoveTrack(ref iters[i]);
                transaction.RemoveQueue.Add(ti);
                
                if(deleteFromFileSystem) {
                    try {
                        File.Delete(ti.Uri.LocalPath);
                    } catch(Exception) {
                        Console.WriteLine("Could not delete file: " + ti.Uri.LocalPath);
                    }
                    
                    // trim empty parent directories
                    try {
                        string old_dir = Path.GetDirectoryName(ti.Uri.LocalPath);
                        while(old_dir != null && old_dir != String.Empty) {
                            Directory.Delete(old_dir);
                            old_dir = Path.GetDirectoryName(old_dir);
                        }
                    } catch(Exception) {}
                }
            }
            
            transaction.Finished += OnLibraryTrackRemoveFinished;
            transaction.Register();
        }
        
        private void OnLibraryTrackRemoveFinished(object o, EventArgs args)
        {
            playlistView.QueueDraw();
            sourceView.QueueDraw();
        }
        
        [GLib.ConnectBefore]
        private void OnButtonPreviousPressed(object o, ButtonPressEventArgs args)
        {
            if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                Previous();
                args.RetVal = true;
            }
        }
        
        // ---------------------------------------------------------------------------------
        // ActionManager Callbacks --- All entries here should be in order with the MainMenu
        // defined in UIManagerLayout.xml. NO CODE that is not ActionManager related may be
        // added below this section! Trying to keep it clean!
        // ---------------------------------------------------------------------------------

        // --- Music Menu ---

        private void OnNewPlaylistAction(object o, EventArgs args)
        {
            Playlist pl = new Playlist(Playlist.UniqueName);
            pl.Saved += OnPlaylistSaved;
            pl.Save();
        }
        
        private void OnImportFolderAction(object o, EventArgs args)
        {
            ImportWithFileSelector();
        }
        
        private void OnImportFilesAction(object o, EventArgs args)
        {
            FileChooserDialog chooser = new FileChooserDialog(
                Catalog.GetString("Import Files to Library"),
                null,
                FileChooserAction.Open
            );
            
            try {
                 chooser.SetCurrentFolderUri(Globals.Configuration.Get(GConfKeys.LastFileSelectorUri) as string);
            } catch(Exception) {
                 chooser.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            }
            
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            
            chooser.SelectMultiple = true;
            chooser.DefaultResponse = ResponseType.Ok;
            
            if(chooser.Run() == (int)ResponseType.Ok) {
                ImportManager.Instance.QueueSource(chooser.Uris);
            }
            
            Globals.Configuration.Set(GConfKeys.LastFileSelectorUri, chooser.CurrentFolderUri);
            
            chooser.Destroy();
        }
        
        private void OnImportCDAction(object o, EventArgs args)
        {
            ArrayList list = new ArrayList();
            
            foreach(object [] node in playlistModel) {
                if(node[0] is AudioCdTrackInfo && ((AudioCdTrackInfo)node[0]).CanRip) {
                    list.Add(node[0] as AudioCdTrackInfo);
                }
            }
            
            if(list.Count > 0) {
                RipTransaction trans = new RipTransaction();
                trans.HaveTrackInfo += OnRipTransactionTrackRipped;
                foreach(AudioCdTrackInfo track in list) {
                    trans.QueueTrack(track);
                }
                trans.Run();
            } else {
                HigMessageDialog dialog = new HigMessageDialog(WindowPlayer, DialogFlags.Modal, 
                    MessageType.Info, ButtonsType.Ok, 
                    Catalog.GetString("Invalid Selection"),
                    Catalog.GetString("You must select at least one track to import.")
                );
                dialog.Run();
                dialog.Destroy();
            }
        }
        
        private void OnWriteCDAction(object o, EventArgs args) 
        {
            if(playlistView.Selection.CountSelectedRows() <= 0) {
                return;
            }
        
            BurnCore burnCore = new BurnCore(BurnCore.DiskType.Audio);
        
            foreach(TreePath path in playlistView.Selection.GetSelectedRows()) {
                burnCore.AddTrack(playlistModel.PathTrackInfo(path));
            }
            
            burnCore.Burn();
        }
        
        private void OnSyncDapAction(object o, EventArgs args)
        {
            if(!(sourceView.HighlightedSource is DapSource)) {
                return;
            }
                
            DapSource dapSource = sourceView.HighlightedSource as DapSource;
        
            if(dapSource == null) {
                return;
            }
        
            HigMessageDialog md = new HigMessageDialog(WindowPlayer, 
                DialogFlags.DestroyWithParent, MessageType.Question,
                // Translators: {0} is the name of the DAP device (i.e. 'iPod')
                String.Format(Catalog.GetString("Synchronize {0}"), dapSource.Device.GenericName),
                String.Format(Catalog.GetString("You have made changes to your {0}. Please choose " +
                    "a method for updating the contents of your {0}.\n\n" + 
                    "<big>\u2022</big> <i>Synchronize Library</i>: synchronize Banshee library to {0}\n" +
                    "<big>\u2022</big> <i>Save Manual Changes</i>: save only the manual changes you made"), 
                    dapSource.Device.GenericName) + (dapSource.Device.GenericName.ToLower() == "ipod" ?
                    ("\n\n" + 
                    Catalog.GetString("<b>Warning:</b> Actions will alter or erase existing iPod contents and " +
                    "may cause incompatability with iTunes!")) : ""),
                Catalog.GetString("Synchronize Library"));
            
            md.AddButton(Catalog.GetString("Save Manual Changes"), Gtk.ResponseType.Apply, true);
            md.Image = dapSource.Device.GetIcon(48);
            md.Icon = md.Image;
            
            switch(md.Run()) {
                case (int)ResponseType.Ok:
                    dapSource.Device.Save(Globals.Library.Tracks.Values);
                    break;
                case (int)ResponseType.Apply:
                    dapSource.Device.Save();
                    break;
            }

            md.Destroy();
        }
        
        private void OnEjectSelectedSourceAction(object o, EventArgs args)
        {
            EjectSource(sourceView.HighlightedSource);
        }

        private void OnSelectedSourcePropertiesAction(object o, EventArgs args)
        {
            sourceView.HighlightedSource.ShowPropertiesDialog();
        }
        
        private void OnQuitAction(object o, EventArgs args)
        {
            Quit();
        }
        
        // --- Edit Menu ---
        
        private void OnRemoveSongsAction(object o, EventArgs args)
        {
            RemoveSongs(false);
        }
        
        private void OnDeleteSongsFromDriveAction(object o, EventArgs args)
        {
            RemoveSongs(true);
        }
        
        private void OnRenameSourceAction(object o, EventArgs args)
        {
            Source source = sourceView.HighlightedSource;
            
            if(source == null || !source.CanRename) {
                 return;
            }
            
            InputDialog input;
            
            if(source.Type == SourceType.Playlist) {
                input = new InputDialog(
                    Catalog.GetString("Rename Playlist"),
                    Catalog.GetString("Enter new playlist name"),
                    Gdk.Pixbuf.LoadFromResource("playlist-icon-large.png"), source.Name);
            } else if(source.Type == SourceType.Dap) {
                DapSource dap_source = source as DapSource;
                input = new InputDialog(
                    Catalog.GetString("Rename Device"),
                    Catalog.GetString("Enter new name for your device"),
                    dap_source.Device.GetIcon(48), source.Name);
            } else {
                return;
            }
                
            string newName = input.Execute();
            if(newName != null) {
                source.Rename(newName);
            }
            
            sourceView.QueueDraw();
        }

        private void OnDeletePlaylistAction(object o, EventArgs args)
        {
            Source source = sourceView.HighlightedSource;
            
            if(source == null || source.Type != SourceType.Playlist) {
                return;
            }
                
            Playlist.Delete(source.Name);
            
            TreeIter iter = TreeIter.Zero;
            ListStore store = sourceView.Model as ListStore;
            for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                if(!store.IterNthChild(out iter, i))
                    continue;
                
                object obj = store.GetValue(iter, 0);
                
                if(!(obj is PlaylistSource))
                    continue;
                    
                PlaylistSource currSource = obj as PlaylistSource;
                if(currSource.Name == source.Name) {
                    store.Remove(ref iter);
                    break;
                }
            }
            
            sourceView.SelectLibrary();
        }
        
        private void OnSelectAllAction(object o, EventArgs args)
        {
            // Don't steal "Ctrl+A" from the search entry
            if (WindowPlayer.Focus is Entry &&
                Gtk.Global.CurrentEvent is Gdk.EventKey) {
                Gtk.Bindings.ActivateEvent(WindowPlayer.Focus, (Gdk.EventKey)Gtk.Global.CurrentEvent);
                return;
            }

            playlistView.Selection.SelectAll();
        }
        
        private void OnSelectNoneAction(object o, EventArgs args)
        {
            playlistView.Selection.UnselectAll();
        }
        
        private void OnPropertiesAction(object o, EventArgs args)
        {
            TrackProperties propEdit = new TrackProperties(playlistView.SelectedTrackInfoMultiple);
            propEdit.Saved += delegate(object o, EventArgs args) {
                playlistView.QueueDraw();
            };
        }
        
        private void OnPreferencesAction(object o, EventArgs args)
        {
            new PreferencesWindow();
        }
        
        // -- Playback Menu ---
        
        private void OnPlayPauseAction(object o, EventArgs args)
        {
            PlayPause();
        }
        
        private void OnPreviousAction(object o, EventArgs args)
        {
            if(PlayerEngineCore.ActivePlayer.Position < 3) {
                Previous();
            } else {
                PlayerEngineCore.ActivePlayer.Position = 0;
            }
        }
        
        private void OnNextAction(object o, EventArgs args)
        {
            Next();
        }
        
        private void OnSeekForwardAction(object o, EventArgs args)
        {
            PlayerEngineCore.ActivePlayer.Position += SkipDelta;
        }
        
        private void OnSeekBackwardAction(object o, EventArgs args)
        {
            PlayerEngineCore.ActivePlayer.Position -= SkipDelta;
        }
        
        private void SetRepeatMode(RepeatMode mode)
        {
            playlistModel.Repeat = mode;
            Globals.Configuration.Set(GConfKeys.PlaylistRepeat, (int)mode);
        }
        
        private void OnRepeatNoneAction(object o, EventArgs args)
        {
            SetRepeatMode(RepeatMode.None);
        }
        
        private void OnRepeatAllAction(object o, EventArgs args)
        {
            SetRepeatMode(RepeatMode.All);
        }
        
        private void OnRepeatSingleAction(object o, EventArgs args)
        {
            SetRepeatMode(RepeatMode.Single);
        }
        
        private void OnShuffleAction(object o, EventArgs args)
        {
            ToggleAction action = o as ToggleAction;
            playlistModel.Shuffle = action.Active;
            Globals.Configuration.Set(GConfKeys.PlaylistShuffle, action.Active);
        }
        
        // --- View Menu ---
        
        private void OnShowCoverArtAction(object o, EventArgs args)
        {
            ToggleAction action = o as ToggleAction;
            cover_art_view.Enabled = action.Active;
            Globals.Configuration.Set(GConfKeys.ShowCoverArt, action.Active);
        }
        
        private bool is_fullscreen = false;
        private void OnFullScreenAction(object o, EventArgs args)
        {
            if(is_fullscreen) {
                WindowPlayer.Unfullscreen();
                is_fullscreen = false;
            } else {
                WindowPlayer.Fullscreen();
                is_fullscreen = true;
            } 
        }
        
        private void OnColumnsAction(object o, EventArgs args)
        {
            playlistView.ColumnChooser();
        }
        
        private LogCoreViewer log_viewer = null;
        private void OnLoggedEventsAction(object o, EventArgs args)
        {
            if(log_viewer == null) {
                log_viewer = new LogCoreViewer(LogCore.Instance, WindowPlayer);
                
                log_viewer.Response += delegate(object o, ResponseArgs args) {
                    log_viewer.Hide();
                };
                
                log_viewer.DeleteEvent += delegate(object o, DeleteEventArgs args) {
                    log_viewer.Destroy();
                    log_viewer = null;
                };
            }
            
            log_viewer.Show();
        }
        
        // --- Help Menu ---
        
        private void OnVersionInformationAction(object o, EventArgs args)
        {
            VersionInformationDialog dialog = new VersionInformationDialog();
            dialog.Run();
            dialog.Destroy();
        }
        
        private void OnAboutAction(object o, EventArgs args)
        {
            new AboutBox();
        }
    }
}
