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
using Mono.Unix;
using Gtk;
using Gdk;
using Glade;
using System.IO;

using Sql;

namespace Banshee
{
    public class PlayerUI
    {
        private Glade.XML gxml;
        private Glade.XML gxmlPlaylistMenu;
        private Glade.XML gxmlSourceMenu;

        [Widget] private Gtk.Window WindowPlayer;
        [Widget] private Gtk.Image ImagePrevious;
        [Widget] private Gtk.Image ImageNext;
        [Widget] private Gtk.Image ImagePlayPause;
        [Widget] private Gtk.Image ImageBurn;
        [Widget] private Gtk.Image ImageRip;
        [Widget] private Gtk.HScale ScaleTime;
        [Widget] private Gtk.Label LabelInfo;
        [Widget] private Gtk.Label LabelStatusBar;
        [Widget] private HPaned SourceSplitter;
        [Widget] private Button HeaderCycleButton;

        private PlaylistModel playlistModel;

        private VolumeButton volumeButton;
        private PlaylistView playlistView;
        private SourceView sourceView;
        private TrackInfo activeTrackInfo;
        private NotificationAreaIcon trayIcon;
        private ImageAnimation spinner;
        private LibraryTransactionStatus libraryTransactionStatus;
        private TrackInfoHeader trackInfoHeader;
        private SimpleNotebook headerNotebook;
        private SearchEntry searchEntry;
        private Tooltips toolTips;
        private Hashtable playlistMenuMap;
        private ProgressBar ipodDiskUsageBar;
        private Viewport sourceViewLoadingVP;
        private Button ipodSyncButton;
        private CoverArtThumbnail cover_art;
        
        private bool incrementedCurrentSongPlayCount;
    
        public Gtk.Window Window
        {
            get {
                return WindowPlayer;
            }
        }
        
        private IpodCore ipodCore = Core.Instance.IpodCore;
        private AudioCdCore audioCdCore = Core.Instance.AudioCdCore;
        
        private long plLoaderMax, plLoaderCount;
        private bool startupLoadReady = false;
        private bool tickFromEngine = false;
        private uint setPositionTimeoutId;
        private bool updateEnginePosition = true;
        private int clickX, clickY;

        private int ipodDiskUsageTextViewState;

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

        public PlayerUI() 
        {
            Catalog.Init(ConfigureDefines.GETTEXT_PACKAGE, ConfigureDefines.LOCALE_DIR);
        
            gxml = new Glade.XML(null, "player.glade", "WindowPlayer", null);
            gxml.Autoconnect(this);

            ResizeMoveWindow();
            BuildWindow();   
            InstallTrayIcon();
            
            Core.Instance.DBusServer.RegisterObject(
                new BansheeCore(Window, this, Core.Instance), "/org/gnome/Banshee/Core");
                        
            WindowPlayer.Show();
            
            Core.Instance.Player.Iterate += OnPlayerTick;
            Core.Instance.Player.EndOfStream += OnPlayerEos;    
            
            if(Core.Instance.Player != Core.Instance.AudioCdPlayer) {
                Core.Instance.AudioCdPlayer.Iterate += OnPlayerTick;
                Core.Instance.AudioCdPlayer.EndOfStream += OnPlayerEos;    
            }
            
            Core.Instance.AudioCdCore.DiskRemoved += OnAudioCdCoreDiskRemoved;
            Core.Instance.AudioCdCore.Updated += OnAudioCdCoreUpdated;
            
            Core.Instance.IpodCore.DeviceAdded += OnIpodCoreDeviceAdded;
            
            LoadSettings();
            Core.Instance.PlayerInterface = this;
            
            Core.Log.Updated += OnLogCoreUpdated;
            
            InitialLoadTimeout();
            //GLib.Timeout.Add(500, InitialLoadTimeout);
    
            Gtk.Application.Run();
        }
                  
        private bool InitialLoadTimeout()
        {
            ConnectToLibraryTransactionManager();
            Core.Library.Reloaded += OnLibraryReloaded;
            Core.Library.ReloadLibrary();
            
            foreach(IPod.Device device in Core.Instance.IpodCore.Devices) {
                 device.Changed += OnIpodDeviceChanged;
                 CheckIpodForNew(device);
            }
            
            if(Core.ArgumentQueue.Contains("audio-cd")) {
                SelectAudioCd(Core.ArgumentQueue["audio-cd"]);
            }
            
            return false;
        }
          
        // ---- Setup/Initialization Routines ----
          
        private void ResizeMoveWindow()
        {
            int x = 0, y = 0, width = 0, height = 0;
              
            try {
                x = (int)Core.GconfClient.Get(GConfKeys.WindowX);
                y = (int)Core.GconfClient.Get(GConfKeys.WindowY); 
                width = (int)Core.GconfClient.Get(GConfKeys.WindowWidth);
                height = (int)Core.GconfClient.Get(GConfKeys.WindowHeight);
            } catch(GConf.NoSuchKeyException e) {
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
        }   
          
        private void BuildWindow()
        {
            // Icons and Pixbufs
            WindowPlayer.Icon = ThemeIcons.WindowManager;
            
            ImagePrevious.SetFromStock("media-prev", IconSize.LargeToolbar);
            ImageNext.SetFromStock("media-next", IconSize.LargeToolbar);
            ImagePlayPause.SetFromStock("media-play", IconSize.LargeToolbar);
            
            ImageBurn.SetFromStock("media-burn", IconSize.LargeToolbar);
            ImageRip.SetFromStock("media-rip", IconSize.LargeToolbar);

            gxml["ButtonBurn"].Visible = true;
                
            ((Gtk.Image)gxml["ImageShuffle"]).Pixbuf = 
                Gdk.Pixbuf.LoadFromResource("media-shuffle.png");
            ((Gtk.Image)gxml["ImageRepeat"]).Pixbuf = 
                Gdk.Pixbuf.LoadFromResource("media-repeat.png");
            ((Gtk.Image)gxml["ImageIpodSync"]).Pixbuf = 
                Gdk.Pixbuf.LoadFromResource("source-ipod-regular.png");
            
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
            
            // Volume Button
            volumeButton = new VolumeButton();
            ((Gtk.Alignment)gxml["VolumeButtonContainer"]).Add(volumeButton);
            
            volumeButton.Visible = true;
            volumeButton.VolumeChanged += 
                new VolumeButton.VolumeChangedHandler(OnVolumeScaleChanged);

            // Cover Art Thumbnail
            cover_art = new CoverArtThumbnail(36);
            (gxml["CoverArtContainer"] as Container).Add(cover_art);
            cover_art.Hide();

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
            playlistView.ButtonPressEvent += OnPlaylistViewButtonPressEvent;
            playlistView.MotionNotifyEvent += OnPlaylistViewMotionNotifyEvent;
            playlistView.ButtonReleaseEvent += OnPlaylistViewButtonReleaseEvent;
            playlistView.DragDataReceived += OnPlaylistViewDragDataReceived;
            playlistView.DragDataGet += OnPlaylistViewDragDataGet;
            playlistView.DragDrop += OnPlaylistViewDragDrop;    
                
            sourceView.SelectLibrary();
                
            playlistView.EnableModelDragSource(
                Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                playlistViewSourceEntries, 
                DragAction.Copy | DragAction.Move);
        
            playlistView.EnableModelDragDest( 
                playlistViewDestEntries, 
                DragAction.Copy | DragAction.Move);
            
            // Ipod Container
            HBox box = new HBox();
            box.Spacing = 5;
            (gxml["IpodContainer"] as Container).Add(box);
            ipodDiskUsageBar = new ProgressBar();
            box.PackStart(ipodDiskUsageBar, false, false, 0);
            ipodDiskUsageBar.ShowAll();
            
            HBox syncBox = new HBox();
            syncBox.Spacing = 3;
            ipodSyncButton = new Button(syncBox);
            Label syncLabel = new Label(Catalog.GetString("Update iPod"));
            ipodSyncButton.Clicked += OnIpodSyncClicked;
            syncBox.PackStart(new Gtk.Image("gtk-copy", IconSize.Menu), false, false, 0);
            syncBox.PackStart(syncLabel, true, true, 0);
            box.PackStart(ipodSyncButton, false, false, 0);
            ipodSyncButton.ShowAll();
            
            Button ipodPropertiesButton = new Button(
                new Gtk.Image("gtk-properties", IconSize.Menu));
            ipodPropertiesButton.Clicked += OnIpodPropertiesClicked;
            box.PackStart(ipodPropertiesButton, false, false, 0);
            ipodPropertiesButton.ShowAll();
            
            Button ipodEjectButton = new Button(new Gtk.Image("media-eject",
                IconSize.Menu));
            ipodEjectButton.Clicked += OnIpodEjectClicked;
            box.PackStart(ipodEjectButton, false, false, 0);
            ipodEjectButton.ShowAll();
            
            (gxml["LeftContainer"] as VBox).PackStart(new ActiveUserEventsManager(), false, false, 0);
            
           // gxml["IpodContainer"].Visible = false;
            
            // Misc
            SetInfoLabel(Catalog.GetString("Idle"));

            // Window Events
            WindowPlayer.KeyPressEvent += OnKeyPressEvent;
            WindowPlayer.ConfigureEvent += OnWindowPlayerConfigureEvent;
            
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
            ((HBox)gxml["PlaylistHeaderBox"]).PackStart(searchEntry, 
                false, false, 0);
                
            toolTips = new Tooltips();
            SetTip(gxml["ButtonNewPlaylist"], Catalog.GetString("Create New Playlist"));
            SetTip(gxml["ToggleButtonShuffle"], Catalog.GetString("Toggle Shuffle Playback Mode"));
            SetTip(gxml["ToggleButtonRepeat"], Catalog.GetString("Toggle Repeat Playback Mode"));
            SetTip(gxml["ButtonTrackProperties"], Catalog.GetString("View Selected Song Information"));
            SetTip(gxml["ButtonBurn"], Catalog.GetString("Burn Selection to CD"));
            SetTip(gxml["ButtonRip"], Catalog.GetString("Rip CD into Library"));
            SetTip(gxml["ButtonPrevious"], Catalog.GetString("Play Previous Song"));
            SetTip(gxml["ButtonPlayPause"], Catalog.GetString("Play/Pause Current Song"));
            SetTip(gxml["ButtonNext"], Catalog.GetString("Play Next Song"));
            SetTip(gxml["ScaleTime"], Catalog.GetString("Current Position in Song"));
            SetTip(volumeButton, Catalog.GetString("Adjust Volume"));
            SetTip(ipodDiskUsageBar, Catalog.GetString("iPod Disk Usage"));
            SetTip(ipodEjectButton, Catalog.GetString("Eject iPod"));
            SetTip(ipodSyncButton, Catalog.GetString("Synchronize Music Library to iPod"));
            SetTip(ipodPropertiesButton, Catalog.GetString("View iPod Properties"));
            
            playlistMenuMap = new Hashtable();
        }
        
        private void SetTip(Widget widget, string tip)
        {
            toolTips.SetTip(widget, tip, tip);
        }
          
        private void InstallTrayIcon()
        {
            try {
                trayIcon = new NotificationAreaIcon();
                trayIcon.ClickEvent += new EventHandler(OnTrayClick);
                //trayIcon.ScrollEvent += new ScrollEventHandler(OnTrayScroll);
                
                trayIcon.PlayItem.Activated += OnButtonPlayPauseClicked;
                trayIcon.NextItem.Activated += OnButtonNextClicked;
                trayIcon.PreviousItem.Activated += OnButtonPreviousClicked;
                trayIcon.ShuffleItem.Activated += OnTrayMenuItemShuffleActivated;
                trayIcon.RepeatItem.Activated += OnTrayMenuItemRepeatActivated;
                trayIcon.ExitItem.Activated += OnMenuQuitActivate;
            } catch(Exception) {
                trayIcon = null;
                DebugLog.Add(Catalog.GetString("Notification Area Icon could not be installed"));
            }
        }
    
        private void LoadSettings()
        {    
            try {
                volumeButton.Volume = (int)Core.GconfClient.Get(GConfKeys.Volume);
            } catch(GConf.NoSuchKeyException) {
                volumeButton.Volume = 80;
            }

            Core.Instance.Player.Volume = (ushort)volumeButton.Volume;
            if(Core.Instance.AudioCdPlayer != Core.Instance.Player) {
                Core.Instance.AudioCdPlayer.Volume = (ushort)volumeButton.Volume;
            }
            
            try {
                ((ToggleButton)gxml["ToggleButtonShuffle"]).Active = (bool)Core.GconfClient.Get(
                    GConfKeys.PlaylistShuffle);
                ((ToggleButton)gxml["ToggleButtonRepeat"]).Active = (bool)Core.GconfClient.Get(
                    GConfKeys.PlaylistRepeat);
            } catch(GConf.NoSuchKeyException) {
                // Default, set in glade file
            }
            
            try {
                SourceSplitter.Position = (int)Core.GconfClient.Get(GConfKeys.SourceViewWidth);
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
            Core.Library.TransactionManager.ExecutionStackChanged += OnLTMExecutionStackChanged;
            Core.Library.TransactionManager.ExecutionStackEmpty += OnLTMExecutionStackEmpty;
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
            if(startupLoadReady) {
                startupLoadReady = false;
                LoadSourceView();
                
                if(Core.ArgumentQueue.Contains("audio-cd")) {
                    sourceView.SelectSource(null);
                    playlistView.Selection.SelectPath(new TreePath("0"));
                    playlistView.PlaySelected();
                    //Next();
                } else {
                    sourceView.SelectLibraryForce();
                }
            }
        }
        
        private void LoadSourceView()
        {        
            sourceView.Sensitive = true;
            ((Gtk.ScrolledWindow)gxml["SourceContainer"]).Remove(sourceViewLoadingVP);
            ((Gtk.ScrolledWindow)gxml["SourceContainer"]).Add(sourceView);
            sourceView.Show();
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
            startupLoadReady = true;
            
            if(Core.Library.Tracks.Count <= 0) {
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
      
        private void Quit()
        {
            ActiveUserEventsManager.Instance.CancelAll();
            playlistView.Shutdown();
            Core.Instance.Player.Dispose();
            Core.GconfClient.Set(GConfKeys.SourceViewWidth, SourceSplitter.Position);
            Core.Instance.Shutdown();
            Application.Quit();
        }
      
        private void SetInfoLabel(string text)
        {
            LabelInfo.Markup = "<span size=\"small\">" + GLib.Markup.EscapeText(text) + "</span>";
        }
          
        public void TogglePlaying()
        {
            if(Core.Instance.Player.Playing) {
                ImagePlayPause.SetFromStock("media-play", IconSize.LargeToolbar);
                Core.Instance.Player.Pause();
                
                if(trayIcon != null) {
                    ((Gtk.Image)trayIcon.PlayItem.Image).SetFromStock("media-play", IconSize.Menu);
                }
            } else {
                ImagePlayPause.SetFromStock("media-pause", IconSize.LargeToolbar);
                Core.Instance.Player.Play();
                if(trayIcon != null) {
                    ((Gtk.Image)trayIcon.PlayItem.Image).SetFromStock("media-pause", IconSize.Menu);
                }
            }
        }
        
        private void UpdateMetaDisplay(TrackInfo ti)
        {
            trackInfoHeader.Artist = ti.DisplayArtist;
            trackInfoHeader.Title = ti.DisplayTitle;
            
            cover_art.Track = ti;
            
            if(trayIcon != null) {
                trayIcon.Tooltip = ti.DisplayArtist + " - " + ti.DisplayTitle;
            }
        }
        
        public void PlayFile(TrackInfo ti)
        {
            Core.Instance.Player.Close();
            
            if(ti.Uri.Scheme == "cdda") {
                Core.Instance.LoadCdPlayer();
            } else {
                Core.Instance.UnloadCdPlayer();
            }
            
            activeTrackInfo = ti;
            Core.Instance.Player.Open(ti);

            incrementedCurrentSongPlayCount = false;
            ScaleTime.Adjustment.Lower = 0;
            ScaleTime.Adjustment.Upper = ti.Duration;

            UpdateMetaDisplay(ti);
            
            TogglePlaying();

            playlistView.QueueDraw();
        }
        
        // ---- Window Event Handlers ----
        
        private void OnWindowPlayerDeleteEvent(object o, DeleteEventArgs args) 
        {
            Quit();
            args.RetVal = true;
        }
        
        [GLib.ConnectBefore]
        private void OnWindowPlayerConfigureEvent(object o, 
            ConfigureEventArgs args)
        {
            int x, y, width, height;

            // Ignore events when maximized.
            if((WindowPlayer.GdkWindow.State & Gdk.WindowState.Maximized) > 0) {
                return;
            }
            
            WindowPlayer.GetPosition(out x, out y);
            WindowPlayer.GetSize(out width, out height);
            
            // might consider putting this in some kind of time delay queue
            // so we're not writing to the gconf client every pixel change
            Core.GconfClient.Set(GConfKeys.WindowX, x);
            Core.GconfClient.Set(GConfKeys.WindowY, y);
            Core.GconfClient.Set(GConfKeys.WindowWidth, width);
            Core.GconfClient.Set(GConfKeys.WindowHeight, height);
        }
        
        [GLib.ConnectBefore]
        private void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            bool handled = false;
            
            switch(args.Event.Key) {
                case Gdk.Key.J:
                case Gdk.Key.j:
                case Gdk.Key.F3:
                    searchEntry.Focus();
                    handled = true;
                    break;
                case Gdk.Key.Left:
                    if(args.Event.State == Gdk.ModifierType.ControlMask) {
                        Core.Instance.Player.Position -= 10;
                        handled = true;
                    }
                    break;
                case Gdk.Key.Right:
                    if(args.Event.State == Gdk.ModifierType.ControlMask) {
                        Core.Instance.Player.Position += 10;
                        handled = true;
                    }
                    break;
            }
            
            args.RetVal = handled;
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
                    OnButtonNextClicked(o, args);
                    break;
                case Gdk.ScrollDirection.Down:
                    OnButtonPreviousClicked(o, args);
                    break;
            }
        }
        
        private void OnTrayMenuItemShuffleActivated(object o, EventArgs args)
        {
            ToggleButton t = (ToggleButton)gxml["ToggleButtonShuffle"];
            t.Active = !t.Active;
        }
        
        private void OnTrayMenuItemRepeatActivated(object o, EventArgs args)
        {
            ToggleButton t = (ToggleButton)gxml["ToggleButtonRepeat"];
            t.Active = !t.Active;
        }

        // ---- Playback Event Handlers ----

        private void OnButtonPlayPauseClicked(object o, EventArgs args)
        {
            if(Core.Instance.Player.Loaded) {
                TogglePlaying();
            } else {
                playlistView.PlaySelected();
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
        
        private void OnButtonPreviousClicked(object o, EventArgs args)
        {
            Previous();
        }
        
        private void OnButtonNextClicked(object o, EventArgs args)
        {
            Next();
        }
        
        private void OnVolumeScaleChanged(int volume)
        {
            Core.Instance.Player.Volume = (ushort)volume;
            Core.GconfClient.Set(GConfKeys.Volume, volume);
        }
        
        // ---- Main Menu Event Handlers ----
        
        private void OnMenuQuitActivate(object o, EventArgs args)
        {
            Quit();
        }
        
        private void OnMenuAboutActivate(object o, EventArgs args)
        {
            new AboutBox();
        }
 
        public void OnVersionInformationItemActivate(object o, EventArgs args)
        {
            VersionInformationDialog dialog = new VersionInformationDialog();
            dialog.Run();
            dialog.Destroy();
        }
       
        private void OnMenuSearchBarActivate(object o, EventArgs args)
        {

        }
                
        private void OnMenuPreferencesActivate(object o, EventArgs args)
        {
            new PreferencesWindow();
        }
        
        private bool is_fullscreen = false;
        private void OnMenuFullScreenActivate(object o, EventArgs args)
        {
            if(is_fullscreen) {
                WindowPlayer.Unfullscreen();
                is_fullscreen = false;
            } else {
                WindowPlayer.Fullscreen();
                is_fullscreen = true;
            }
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
                          String.Format("{0}:{1:00}", activeTrackInfo.Duration / 60, 
                          activeTrackInfo.Duration % 60))
            );    
        }
        
        private void OnPlayerTick(object o, PlayerEngineIterateArgs args)
        {
            if(activeTrackInfo == null) {
                return;
            }
             
            if(Core.Instance.Player.Length > 0 
                && activeTrackInfo.Duration <= 0) {
                activeTrackInfo.Duration = Core.Instance.Player.Length;
                activeTrackInfo.Save();
                playlistView.ThreadedQueueDraw();
                ScaleTime.Adjustment.Upper = activeTrackInfo.Duration;
            }
                
            if(Core.Instance.Player.Length > 0 && 
                Core.Instance.Player.Position > Core.Instance.Player.Length / 2
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
                });
            }
        }
        
        private bool SetPositionTimeoutCallback()
        {
            setPositionTimeoutId = 0;
            Application.Invoke(delegate {
                ScaleTime.Value = Core.Instance.Player.Position;
            });
            
            return false;
        }
        
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
            Core.Instance.Player.Position = (uint)ScaleTime.Value;
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
                 chooser.SetCurrentFolderUri(Core.GconfClient.Get(
                     GConfKeys.LastFileSelectorUri) as string);
            } catch(Exception) {
                 chooser.SetCurrentFolder(Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal));
            }
            
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            chooser.DefaultResponse = ResponseType.Ok;
            
            if(chooser.Run() == (int)ResponseType.Ok) 
                ImportMusic(chooser.Uri);
                
            Core.GconfClient.Set(GConfKeys.LastFileSelectorUri,
                 chooser.CurrentFolderUri);
            
            chooser.Destroy();
        }
        
        private void ImportMusic(string path)
        {
            if(sourceView.SelectedSource.Type == SourceType.Library &&
                searchEntry.Query == String.Empty) {
                playlistModel.AddFile(path);
            } else {
                FileLoadTransaction loader = new FileLoadTransaction(path);
                loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
                loader.Register();
            }
        }
        
        private void OnLoaderHaveTrackInfo(object o, HaveTrackInfoArgs args)
        {
            sourceView.QueueDraw();
            playlistView.QueueDraw();
        }
        
        private void ImportHomeDirectory()
        {
            ImportMusic(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        }

        private void OnMenuImportFolderActivate(object o, EventArgs args)
        {
            ImportWithFileSelector();
        }
        
        private void OnMenuImportFilesActivate(object o, EventArgs args)
        {
            FileChooserDialog chooser = new FileChooserDialog(
                Catalog.GetString("Import Files to Library"),
                null,
                FileChooserAction.Open
            );
            
            try {
                 chooser.SetCurrentFolderUri(Core.GconfClient.Get(
                     GConfKeys.LastFileSelectorUri) as string);
            } catch(Exception) {
                 chooser.SetCurrentFolder(Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal));
            }
            
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            
            chooser.SelectMultiple = true;
            chooser.DefaultResponse = ResponseType.Ok;
            
            if(chooser.Run() == (int)ResponseType.Ok) {
                foreach(string path in chooser.Uris) {
                    ImportMusic(path);
                }
            }
            
            Core.GconfClient.Set(GConfKeys.LastFileSelectorUri,
                 chooser.CurrentFolderUri);
            
            chooser.Destroy();
        }
                
        private void OnMenuTrackPropertiesActivate(object o, EventArgs args)
        {
            OnItemPropertiesActivate(o, args);
        }
        
        private void OnMenuNewPlaylistActivate(object o, EventArgs args)
        {
            Playlist pl = new Playlist(Playlist.UniqueName);
            pl.Saved += OnPlaylistSaved;
            pl.Save();
        }
        
        private void OnPlaylistSaved(object o, PlaylistSavedArgs args)
        {    
            sourceView.AddPlaylist(args.Name);
        }
        
        private void OnPlaylistObjectUpdated(object o, EventArgs args)
        {
            sourceView.ThreadedQueueDraw();
        }
        
        private void OnMenuNewSmartPlaylistActivate(object o, EventArgs args)
        {
            new SqlBuilderUI();
        }
        
        private void OnMenuPlaylistRemoveActivate(object o, EventArgs args)
        {
            
        }
        
        private void OnMenuPlaylistPropertiesActivate(object o, EventArgs args)
        {
            
        }
        
        private void OnSourceChanged(object o, EventArgs args)
        {
            if(Core.InMainThread) {
                HandleSourceChanged(o, args);
            } else {
                Application.Invoke(HandleSourceChanged);
            }
        }
        
        private void HandleSourceChanged(object o, EventArgs args)
        {
            Source source = sourceView.SelectedSource;
            if(source == null) {
                return;
            }
                
            searchEntry.CancelSearch(false);
                
            if(source.Type == SourceType.Library) {
                playlistModel.LoadFromLibrary();
                playlistModel.Source = source;
               
                (gxml["ViewNameLabel"] as Label).Markup = 
                    String.Format(Catalog.GetString("<b>{0}'s Music Library</b>"),
                    GLib.Markup.EscapeText(Core.Instance.UserFirstName));
            } else if(source.Type == SourceType.Ipod) {
                playlistModel.Clear();
                playlistModel.Source = source;
                
                IpodSource ipodSource = source as IpodSource;
                playlistModel.LoadFromIpodSource(ipodSource);
                
                UpdateIpodDiskUsageBar(ipodSource);
            } else if(source.Type == SourceType.AudioCd) {
                playlistModel.Clear();
                playlistModel.Source = source;
                
                AudioCdSource cdSource = source as AudioCdSource;
                playlistModel.LoadFromAudioCdSource(cdSource);
            } else {
                playlistModel.LoadFromPlaylist(source.Name);
                playlistModel.Source = source;
            }
            
            (gxml["ViewNameLabel"] as Label).Markup = 
                "<b>" + GLib.Markup.EscapeText(source.Name) + "</b>";

            gxml["ButtonRip"].Visible = source.Type == SourceType.AudioCd;
            gxml["ButtonBurn"].Visible = source.Type != SourceType.AudioCd;
            gxml["IpodSyncButton"].Visible = source.Type == SourceType.Ipod;
            (gxml["HeaderActionButtonBox"] as HBox).Spacing = (
                gxml["ButtonBurn"].Visible && gxml["IpodSyncButton"].Visible) ?  10 : 0;
            
            if(source.Type == SourceType.Ipod) {
                gxml["IpodContainer"].ShowAll();
                IpodSource ipodSource = source as IpodSource;
                ipodSyncButton.Visible = ipodSource.Device.CanWrite;
            } else {
                gxml["IpodContainer"].Visible = false;
            }     
                 
            gxml["SearchLabel"].Sensitive = source.Type == SourceType.Ipod 
                 || source.Type == SourceType.Library;
            searchEntry.Sensitive = gxml["SearchLabel"].Sensitive;
            playlistView.SyncColumn.Visible = source.Type == SourceType.Ipod;
            playlistView.RipColumn.Visible = source.Type == SourceType.AudioCd;
            
            playlistModel.ImportCanUpdate = source.Type == SourceType.Library
                && searchEntry.Query == String.Empty;
                
            if(source.Type != SourceType.Ipod)
                playlistView.Sensitive = true;
            else if((source as IpodSource).IsSyncing)
                playlistView.Sensitive = false;
            else
                playlistView.Sensitive = true;
        }
        
        private void UpdateIpodDiskUsageBar(IpodSource ipodSource)
        {
            Application.Invoke(delegate {
            ipodDiskUsageBar.Fraction = ipodSource.DiskUsageFraction;
            ipodDiskUsageBar.Text = ipodSource.DiskUsageString;
            string tooltip = ipodSource.DiskUsageString + " (" +
                ipodSource.DiskAvailableString + ")";
            toolTips.SetTip(ipodDiskUsageBar, tooltip, tooltip);
            });
        }
        
        private void CheckIpodForNew(IPod.Device device)
        {
          if(device.IsNew) {
              Application.Invoke(delegate {
                new IpodNewDialog(device);
                 sourceView.QueueDraw();
              });
           }
        }
        
        private void OnIpodDeviceChanged(object o, EventArgs args)
        {
			Application.Invoke (delegate {
				IPod.Device device = o as IPod.Device;
				
				foreach(object [] obj in (sourceView.Model as ListStore)) {
					if(obj[0] is IpodSource && (obj[0] as IpodSource).Device == device) {
						(obj[0] as IpodSource).SetSourceName(device.Name);
						sourceView.QueueDraw();
					}
				}
				
				if(playlistModel.Source is IpodSource && (playlistModel.Source as IpodSource).Device == device) {
					UpdateIpodDiskUsageBar(playlistModel.Source as IpodSource);
					(gxml["ViewNameLabel"] as Label).Markup = "<b>" 
						+ GLib.Markup.EscapeText(device.Name) + "</b>";
					sourceView.QueueDraw();
				}
			});
        }
        
        private void OnIpodCoreDeviceAdded(object o, IpodDeviceArgs args)
        {
            args.Device.Changed += OnIpodDeviceChanged;
            CheckIpodForNew(args.Device);
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
            if(source == null)
                return;
            
            Application.Invoke(delegate {
                playlistView.QueueDraw();
            
                if(source.Type == SourceType.AudioCd 
                    && playlistModel.FirstTrack != null 
                    && playlistModel.FirstTrack.GetType() == 
                    typeof(AudioCdTrackInfo)) {
                    AudioCdSource cdSource = source as AudioCdSource;
                    AudioCdTrackInfo track = playlistModel.FirstTrack 
                        as AudioCdTrackInfo;
                    
                    if(cdSource.Disk.DeviceNode == track.Device) 
                        (gxml["ViewNameLabel"] as Label).Markup = 
                        "<b>" + GLib.Markup.EscapeText(cdSource.Disk.Title) + "</b>";
                }            
            });
        }
        
        private void OnIpodPropertiesClicked(object o, EventArgs args)
        {
            if(sourceView.SelectedSource.Type != SourceType.Ipod)
                return;
            
            ShowSourceProperties(sourceView.SelectedSource);
        }
        
        private void OnIpodSyncButtonClicked(object o, EventArgs args)
        {
            OnIpodSyncClicked(o, args);
        }
        
        private void OnIpodSyncClicked(object o, EventArgs args)
        {
            if(sourceView.SelectedSource.Type != SourceType.Ipod)
                return;
        
            IpodSource ipodSource = sourceView.SelectedSource as IpodSource;
            IpodSync sync = null;
            
            if(!ipodSource.Device.CanWrite)
                 return;
        
            if(ipodSource.NeedSync) {
                HigMessageDialog md = new HigMessageDialog(WindowPlayer, 
                    DialogFlags.DestroyWithParent, MessageType.Question,
                    Catalog.GetString("Synchronize iPod"),
                    Catalog.GetString("You have made changes to your iPod. Please choose " +
                    "a method for updating the contents of your iPod.\n\n" + 
                    "<i>Synchronize Library</i>: synchronize Banshee library to iPod\n" +
                    "<i>Save Manual Changes</i>: save only the manual changes you made\n\n" +
                    "<b>Warning:</b> Actions will alter or erase existing iPod contents and may cause incompatability with iTunes!"),
                    Catalog.GetString("Synchronize Library"));
                    md.AddButton(Catalog.GetString("Save Manual Changes"), 
                        Gtk.ResponseType.Apply, true);
                md.Image = Gdk.Pixbuf.LoadFromResource("ipod-48.png");
                md.Icon = md.Image;
                switch(md.Run()) {
                    case (int)ResponseType.Ok:
                        sync = ipodSource.Sync(true);
                        sync.SyncStarted += OnIpodSyncStarted;
                        sync.SyncCompleted += OnIpodSyncCompleted;
                        sync.StartSync();
                        break;
                    case (int)ResponseType.Apply:
                        sync = ipodSource.Sync(false);
                        sync.SyncStarted += OnIpodSyncStarted;
                        sync.SyncCompleted += OnIpodSyncCompleted;
                        sync.StartSync();
                        break;
                }
                
                md.Destroy();
            } else {
                HigMessageDialog md = new HigMessageDialog(WindowPlayer, 
                    DialogFlags.DestroyWithParent, MessageType.Question,
                    Catalog.GetString("Synchronize iPod"),
                    Catalog.GetString("Are you sure you want to synchronize your iPod " +
                    "with your Banshee library? This will <b>erase</b> the contents of " +
                    "your iPod and then copy the contents of your Banshee library."),
                    Catalog.GetString("Synchronize Library"));
                md.Image = Gdk.Pixbuf.LoadFromResource("ipod-48.png");
                md.Icon = md.Image;
                switch(md.Run()) {
                    case (int)ResponseType.Ok:
                        sync = ipodSource.Sync(true);
                        sync.SyncStarted += OnIpodSyncStarted;
                        sync.SyncCompleted += OnIpodSyncCompleted;
                        sync.StartSync();
                        break;
                }
                
                md.Destroy();
            }
        }
        
        private void OnIpodEjectClicked(object o, EventArgs args)
        {
            EjectSource(sourceView.SelectedSource);
        }
        
        private void OnIpodSyncStarted(object o, EventArgs args)
        {
            if(playlistModel.Source.Type == SourceType.Ipod 
                && (playlistModel.Source as IpodSource).IsSyncing) {
                playlistModel.ClearModel();
                playlistView.Sensitive = false;
            }
        }
        
        private void OnIpodSyncCompleted(object o, EventArgs args)
        {
            if(playlistModel.Source.Type == SourceType.Ipod 
                && !(playlistModel.Source as IpodSource).IsSyncing) {
                playlistView.Sensitive = true;
                playlistModel.LoadFromIpodSource(
                    (playlistModel.Source as IpodSource));
            }
            
            sourceView.QueueDraw();
            playlistView.QueueDraw();
        }
    
        private void OnToggleButtonShuffleToggled(object o, EventArgs args)
        {
            ToggleButton t = (ToggleButton)o;
            playlistModel.Shuffle = t.Active;
            Core.GconfClient.Set(GConfKeys.PlaylistShuffle, 
                t.Active);
                
            if(trayIcon != null)
                ((Gtk.Image)trayIcon.ShuffleItem.Image).SetFromStock(
                    t.Active ? "gtk-yes" : "gtk-no", IconSize.Menu);
        }
        
        private void OnToggleButtonRepeatToggled(object o, EventArgs args)
        {
            ToggleButton t = (ToggleButton)o;
            playlistModel.Repeat = t.Active;
            Core.GconfClient.Set(GConfKeys.PlaylistRepeat, 
                t.Active);
                
            if(trayIcon != null)
                ((Gtk.Image)trayIcon.RepeatItem.Image).SetFromStock(
                    t.Active ? "gtk-yes" : "gtk-no", IconSize.Menu);
        }
        
        private void OnPlaylistUpdated(object o, EventArgs args)
        {
            long count = playlistModel.Count();
            long tsec = playlistModel.TotalDuration;
            long d, h, m, s;
            
            d = tsec / 86400;
            s = tsec - (86400 * d);
            if(s < 0) {
                d = 0;
                s += 86400;
            }
            
            h = s / 3600;
            s -= 3600 * h;
            m = s / 60;
            s -= m * 60;
        
            string timeDisp = String.Empty;
            
            if(d > 0)
                timeDisp = String.Format(Catalog.GetPluralString("{0} day",
                    "{0} days", (int)d), d) + " ";
            if(d > 0 || h > 0)
                timeDisp += String.Format("{0}:{1}:{2}",
                    h, m.ToString("00"), s.ToString("00"));
            else    
                timeDisp += String.Format("{0}:{1}",
                    m, s.ToString("00"));
        
            Application.Invoke(delegate {
            
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
                string text = String.Format(
                    Catalog.GetPluralString("{0} Item", "{0} Items", 
                        (int)count), count);
                text += ", ";
                text += String.Format(
                    Catalog.GetString("{0} Total Play Time"),
                    timeDisp);
                text += " ";
                LabelStatusBar.Text = text;
            }
                
            });
        }
        
        private void OnLogCoreUpdated(object o, LogCoreUpdatedArgs args)
        {
            Console.WriteLine(args.Entry.ShortMessage + ": " + args.Entry.Details);
        
            if(args.Entry.Type != LogEntryType.UserError)
              return;
              
            HigMessageDialog.RunHigMessageDialog(WindowPlayer, 
              DialogFlags.Modal,
              MessageType.Error,
              ButtonsType.Ok,
              args.Entry.ShortMessage,
              args.Entry.Details);
        }
        
        // PlaylistMenu Handlers
    
        private void OnItemColumnsActivate(object o, EventArgs args)
        {
            playlistView.ColumnChooser();
        }
        
        private void OnMenuColumnsActivate(object o, EventArgs args)
        {
            OnItemColumnsActivate(o, args);
        }
        
        private void OnItemRemoveActivate(object o, EventArgs args)
        {
            RemoveSongs(false);
        }
        
        private void OnItemRemoveFileSystemActivate(object o, EventArgs args)
        {
            RemoveSongs(true);
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
                        "Are you sure you want to remove the selected song from your library <i><b>and</b></i> your drive? This action will permanently delete the file.",
                        "Are you sure you want to remove the selected <b>({0})</b> songs from your library <i><b>and</b></i> your drive? This action will permanently delete the files.",
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
            
            if(playlistModel.Source.Type == SourceType.Ipod) {
                for(i = 0; i < iters.Length; i++) {
                    TrackInfo ti = playlistModel.IterTrackInfo(iters[i]);
                    playlistModel.RemoveTrack(ref iters[i]);
                    
                    IpodTrackInfo iti = ti as IpodTrackInfo;
                    (playlistModel.Source as IpodSource).Remove(iti);
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
        
        private void OnItemPropertiesActivate(object o, EventArgs args)
        {
            TrackProperties propEdit = new TrackProperties(
                playlistView.SelectedTrackInfoMultiple);
            propEdit.Saved += OnTrackPropertyEditorSaved;
        }
        
        private void OnTrackPropertyEditorSaved(object o, EventArgs args)
        {
            playlistView.QueueDraw();
        }
        
        private void OnButtonNewPlaylistClicked(object o, EventArgs args)
        {
            OnMenuNewPlaylistActivate(o, args);
        }
        
        private void OnButtonTrackPropertiesClicked(object o, EventArgs args)
        {
            OnItemPropertiesActivate(o, args);
        }
        
        // SourceMenu Handlers
        
        private uint popupTime;
        
        [GLib.ConnectBefore]
        private void OnSourceViewButtonPressEvent(object o, 
            ButtonPressEventArgs args)
        {
            if(args.Event.Button != 3)
                return;
                
            TreePath path;
            if(!sourceView.GetPathAtPos((int)args.Event.X, 
                (int)args.Event.Y, out path)) {
                args.RetVal = true; 
                return;
            }
            
            sourceView.HighlightPath(path);
            Source source = sourceView.GetSource(path);
            
            if(source.Type == SourceType.Library)
                return;

            if(gxmlSourceMenu == null) {
                gxmlSourceMenu = new Glade.XML(null, "player.glade", 
                    "SourceMenu", null);
                gxmlSourceMenu.Autoconnect(this);
            }
            
            Menu menu = gxmlSourceMenu["SourceMenu"] as Menu;
            MenuItem addSelectedSongs = gxmlSourceMenu["ItemAddSelectedSongs"] as MenuItem;
            MenuItem sourceDuplicate = gxmlSourceMenu["ItemSourceDuplicate"] as MenuItem;
            MenuItem sourceRename = gxmlSourceMenu["ItemSourceRename"] as MenuItem;            
            MenuItem sourceDelete = gxmlSourceMenu["ItemSourceDelete"] as MenuItem;
            MenuItem sourceProperties = gxmlSourceMenu["ItemSourceProperties"] as MenuItem;
            ImageMenuItem ejectItem = gxmlSourceMenu["ItemEject"] as ImageMenuItem;
            
            //addSelectedSongs.Sensitive = source.Type == SourceType.Playlist
            //    && playlistView.Selection.CountSelectedRows() > 0;
            addSelectedSongs.Visible = false;
            
            sourceProperties.Visible = source.Type == SourceType.Ipod;
          
            if(source.CanEject) {
                ejectItem.Image = new Gtk.Image("media-eject", IconSize.Menu);
            }
            
            menu.Popup(null, null, null, 0, args.Event.Time);
            menu.Show();
            
            addSelectedSongs.Visible = source.Type == SourceType.Playlist ||
             source.Type == SourceType.Ipod;
            
            sourceDuplicate.Visible = false;
            ejectItem.Visible = source.CanEject;
            sourceDelete.Visible = !source.CanEject;
            sourceRename.Visible = source.CanRename;
            
            args.RetVal = true;
        }
        
        private void OnItemAddSelectedSongsActivate(object o, EventArgs args)
        {
            Source source = sourceView.HighlightedSource;
            
            if(source == null || source.Type != SourceType.Playlist)
                return;
                
            playlistView.AddSelectedToPlayList(source.Name);
        }
        
        private void OnItemSourceDuplicateActivate(object o, EventArgs args)
        {
    
        }
        
        private void OnItemSourceDeleteActivate(object o, EventArgs args)
        {
            Source source = sourceView.HighlightedSource;
            
            if(source == null || source.Type != SourceType.Playlist)
                return;
                
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
        
        private void OnItemEjectActivate(object o, EventArgs args)
        {
            EjectSource(sourceView.HighlightedSource);
        }
        
        private void OnItemSourcePropertiesActivate(object o, EventArgs args)
        {
            ShowSourceProperties(sourceView.HighlightedSource);
        }
        
        private void OnItemSourceRenameActivate(object o, EventArgs args)
        {
            Source source = sourceView.HighlightedSource;
            
            if(source == null || !source.CanRename)
                 return;
            
            InputDialog input;
            
            if(source.Type == SourceType.Playlist)
             input = new InputDialog(
                Catalog.GetString("Rename Playlist"),
                Catalog.GetString("Enter new playlist name"), 
                "playlist-icon-large.png", source.Name);
            else
              input = new InputDialog(
                  Catalog.GetString("Rename iPod"),
                  Catalog.GetString("Enter new name for your iPod"),
                  "ipod-48.png", source.Name);
                
            string newName = input.Execute();
            if(newName != null)
                source.Rename(newName);

            sourceView.QueueDraw();
        }
        
        private void OnItemRenamePlaylistActivate(object o, EventArgs args)
        {
            OnItemSourceRenameActivate(o, args);
        }
        
        private void OnItemRemoveSongsActivate(object o, EventArgs args)
        {
            RemoveSongs(false);
        }
        
        private void OnItemDeleteSongsFileSystemActivate(object o, EventArgs args)
        {
            RemoveSongs(true);
        }
        
        private void OnItemDeletePlaylistActivate(object o, EventArgs args)
        {
            OnItemSourceDeleteActivate(o, args);
        }
        
        private void OnItemSelectAllActivate(object o, EventArgs args)
        {
            // Don't steal "Ctrl+A" from the search entry
            if (WindowPlayer.Focus is Entry &&
                Gtk.Global.CurrentEvent is Gdk.EventKey) {
                Gtk.Bindings.ActivateEvent(WindowPlayer.Focus, (Gdk.EventKey)Gtk.Global.CurrentEvent);
                return;
            }

            playlistView.Selection.SelectAll();
        }
        
        private void OnItemSelectNoneActivate(object o, EventArgs args)
        {
            playlistView.Selection.UnselectAll();
        }
        
        private void OnSimpleSearch(object o, EventArgs args)
        {
            Source source = sourceView.SelectedSource;
            playlistModel.Clear();
            
            string query = searchEntry.Query;
            string field = searchEntry.Field;
            
            
            playlistModel.ImportCanUpdate = 
                query == null || query == String.Empty;
            
            if(query == null || query == String.Empty) {
                if(source.Type == SourceType.Ipod)
                    playlistModel.LoadFromIpodSource(source as IpodSource);
                else
                    playlistModel.LoadFromLibrary();
                
                return;
            }
            
            query = query.ToLower();
            
            ICollection collection = null;
            
            if(source.Type == SourceType.Ipod) {
                collection = (source as IpodSource).Tracks;
            } else {
                collection = Core.Library.Tracks.Values;
            }
            
            foreach(TrackInfo ti in collection) {
                string match;
                
                try {
                    if(field == Catalog.GetString("Artist Name"))
                        match = ti.Artist;
                    else if(field == Catalog.GetString("Song Name"))
                        match = ti.Title;
                    else if(field == Catalog.GetString("Album Title"))
                        match = ti.Album;
                    else {
                        string [] matches = {
                            ti.Artist,
                            ti.Album,
                            ti.Title
                        };

                        foreach(string m in matches) {
                            if (m == null)
                                continue;
                            
                            string ml = m.ToLower();
                            if(ml.IndexOf(query) >= 0
                               || ml.IndexOf("the " + query) >= 0) {
                                playlistModel.AddTrack(ti);
                                break;
                            }
                        }

                        continue;
                    }
                    
                    match = match.ToLower();
                    if(match.IndexOf(query) >= 0
                        || match.IndexOf("the " + query) >= 0)
                        playlistModel.AddTrack(ti);
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

        private bool PlaylistMenuPopupTimeout(uint time)
        {
            if(gxmlPlaylistMenu == null) {
                gxmlPlaylistMenu = new Glade.XML(null, "player.glade", 
                    "PlaylistMenu", null);
                gxmlPlaylistMenu.Autoconnect(this);
            }
        
            bool sensitive = playlistView.Selection.CountSelectedRows() > 0;
            Menu menu = gxmlPlaylistMenu["PlaylistMenu"] as Menu;
            
            if(sensitive) {
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
                        item.Image = new Gtk.Image(
                            Pixbuf.LoadFromResource("source-playlist.png"));
                        
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
                    for(int j = 0; j < i + 1; j++)
                        box.PackStart(new Gtk.Image(RatingRenderer.Star), 
                            false, false, 0);
                    item.Add(box);
                    item.Name = String.Format("{0}", i + 1);
                    item.Activated += OnItemRatingActivated;
                    ratingMenu.Append(item);
                }
            
                (gxmlPlaylistMenu["ItemAddToPlaylist"] 
                    as MenuItem).Submenu = plMenu;
                (gxmlPlaylistMenu["ItemRating"] 
                    as MenuItem).Submenu = ratingMenu;
            }
        
            menu.ShowAll();
            
            gxmlPlaylistMenu["ItemAddToPlaylist"].Visible = sensitive;
            gxmlPlaylistMenu["ItemRating"].Visible = sensitive;
            gxmlPlaylistMenu["ItemSep"].Visible = sensitive;
            gxmlPlaylistMenu["ItemRemove"].Visible = sensitive;
            gxmlPlaylistMenu["ItemProperties"].Visible = sensitive;
            
            menu.Popup(null, null, null, 0, time);
            
            return false;
        }

        private void OnItemAddToPlaylistActivated(object o, EventArgs args)
        {
            string name = playlistMenuMap[o] as string;
            
            if(name == null)
                return;
                
            playlistView.AddSelectedToPlayList(name);
        }
        
        private void OnItemRatingActivated(object o, EventArgs args)
        {
            uint rating = Convert.ToUInt32((o as Widget).Name);
            foreach(TreePath path in playlistView.Selection.GetSelectedRows())
                playlistModel.PathTrackInfo(path).Rating = rating;
            playlistView.QueueDraw();
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
                
                    if(rawSelectionData != null 
                        && rawSelectionData.Trim().Length > 0)
                        ImportMusic(rawSelectionData);
                        
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
        
        private void OnButtonBurnClicked(object o, EventArgs args)
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
        
        private void OnButtonRipClicked(object o, EventArgs args)
        {
            RipTransaction trans = new RipTransaction();

            foreach(object [] node in playlistModel) {
                if(node[0] is AudioCdTrackInfo && ((AudioCdTrackInfo)node[0]).CanRip) {
                    trans.QueueTrack(node[0] as AudioCdTrackInfo);
                }
            }
            
            if(trans.QueueSize > 0) {
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
        
        private void EjectSource(Source source)
        {
            if(source.CanEject) {
                try {
                    if(source.GetType() == typeof(IpodSource)) {
                        if(activeTrackInfo != null && activeTrackInfo is IpodTrackInfo) {
                            StopPlaying();
                        }
                    }
                        
                    source.Eject();
                    
                    if(source == sourceView.SelectedSource)
                        sourceView.SelectLibrary();
                } catch(Exception e) {
                    HigMessageDialog.RunHigMessageDialog(null, 
                        DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, 
                        Catalog.GetString("Could Not Eject"),
                        e.Message);
                }
            }
        }
        
        private void ShowSourceProperties(Source source)
        {
            switch(source.Type) {
                case SourceType.Ipod:
                    IpodSource ipodSource = source as IpodSource;
                    IPod.Device device = ipodSource.Device;
                    IpodPropertiesDialog propWin = 
                        new IpodPropertiesDialog(ipodSource);
                    propWin.Run();
                    propWin.Destroy();
                    if(propWin.Edited && device.CanWrite)
                        device.Save();
                    source.Rename(device.Name);
                    sourceView.QueueDraw();
                    break;
            }
        }
        
        private void StopPlaying()
        {
            Core.Instance.Player.Close();
            
            ImagePlayPause.SetFromStock("media-play", IconSize.LargeToolbar);
            ScaleTime.Adjustment.Lower = 0;
            ScaleTime.Adjustment.Upper = 0;
            ScaleTime.Value = 0;
            SetInfoLabel(Catalog.GetString("Idle"));
            trackInfoHeader.SetIdle();
            activeTrackInfo = null;
            
            if(trayIcon != null)
                trayIcon.Tooltip = Catalog.GetString("Banshee - Idle");
        }

        public void SelectAudioCd(string device)
        {
            Console.WriteLine("Selecting CD");
        }
        
        public TrackInfo ActiveTrackInfo
        {
            get {
                return activeTrackInfo;
            }
        }
    }
}
