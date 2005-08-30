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
using Gtk;
using Gdk;
using Glade;
using Mono.Posix;

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
				Dnd.TargetSource,
				Dnd.TargetPlaylist,
				Dnd.TargetUriList
			};
			
		private static TargetEntry [] playlistViewDestEntries = 
			new TargetEntry [] {
				Dnd.TargetPlaylist,
				Dnd.TargetSource,
				Dnd.TargetUriList
			};

		private static TargetEntry [] sourceViewSourceEntries = 
			new TargetEntry [] {
				Dnd.TargetPlaylist
			};
			
		private static TargetEntry [] sourceViewDestEntries = 
			new TargetEntry [] {
				Dnd.TargetSource
			};

        public PlayerUI() 
        {
			Catalog.Init("banshee", ConfigureDefines.LOCALE_DIR);
		
			gxml = new Glade.XML(null, "player.glade", "WindowPlayer", null);
			gxml.Autoconnect(this);

			ResizeMoveWindow();
			BuildWindow();   
			InstallTrayIcon();
			WindowPlayer.Show();
			
			Core.Instance.Player.Iterate += OnPlayerTick;
			Core.Instance.Player.EndOfStream += OnPlayerEos;		
			
			LoadSettings();
			Core.Instance.PlayerInterface = this;
			
			GLib.Timeout.Add(500, InitialLoadTimeout);
	
			Gdk.Threads.Enter();
			Gtk.Application.Run();
			Gdk.Threads.Leave();
      	}
      			
      	private bool InitialLoadTimeout()
      	{
      		ConnectToLibraryTransactionManager();
			Core.Library.Reloaded += OnLibraryReloaded;
			Core.Library.ReloadLibrary();
			return false;
      	}
      	
      	// ---- Setup/Initialization Routines ----
      	
      	private void ResizeMoveWindow()
      	{
      		int x = 0, y = 0, width = 0, height = 0;
      		
      		try {
				x = (int)Core.GconfClient.Get(
					GConfKeys.WindowX);
				y = (int)Core.GconfClient.Get(
					GConfKeys.WindowY); 
				width = (int)Core.GconfClient.Get(
					GConfKeys.WindowWidth);
				height = (int)Core.GconfClient.Get(
					GConfKeys.WindowHeight);
			} catch(GConf.NoSuchKeyException e) {
				width = 800;
				height = 600;
				x = 10;
				y = 10;
			}
      	
      		if(width != 0 && height != 0) {
				WindowPlayer.Resize(width, height);
			}

			if(x == 0 && y == 0)
				WindowPlayer.SetPosition(Gtk.WindowPosition.Center);
			else
				WindowPlayer.Move(x, y);
		}
      	
      	private void BuildWindow()
      	{
			// Icons and Pixbufs
      		WindowPlayer.Icon = Gdk.Pixbuf.LoadFromResource("banshee-icon.png");
			
			ImagePrevious.SetFromStock("media-prev", IconSize.LargeToolbar);
			ImageNext.SetFromStock("media-next", IconSize.LargeToolbar);
			ImagePlayPause.SetFromStock("media-play", IconSize.LargeToolbar);
			
			ImageBurn.SetFromStock("media-burn", IconSize.LargeToolbar);
			
			gxml["ButtonBurn"].Visible = Environment.GetEnvironmentVariable("BANSHEE_BURN_ENABLE") != null;
				
			((Gtk.Image)gxml["ImageShuffle"]).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("media-shuffle.png");
			((Gtk.Image)gxml["ImageRepeat"]).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("media-repeat.png");
			
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

			// Source View
			Label sourceViewLoading = new Label();
			sourceViewLoading.Yalign = 0.15f;
			sourceViewLoading.Xalign = 0.5f;
			sourceViewLoading.Markup = Catalog.GetString("<big><i>Loading...</i></big>");
			sourceViewLoadingVP = new Viewport();
			sourceViewLoadingVP.ShadowType = ShadowType.None;
			sourceViewLoadingVP.Add(sourceViewLoading);
			sourceViewLoadingVP.ShowAll();
			((Gtk.ScrolledWindow)gxml["SourceContainer"]).Add(sourceViewLoadingVP);
			
			sourceView = new SourceView();
			sourceView.SourceChanged += OnSourceChanged;
			sourceView.ButtonPressEvent += OnSourceViewButtonPressEvent;
			sourceView.DragMotion += OnSourceViewDragMotion;
			sourceView.DragDataReceived += OnSourceViewDragDataReceived;
			sourceView.Sensitive = false;

			Gtk.Drag.SourceSet(sourceView, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				sourceViewSourceEntries, 
				DragAction.Copy | DragAction.Move);
		
			Gtk.Drag.DestSet(sourceView, 
				DestDefaults.All, sourceViewDestEntries, 
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
			playlistView.DragMotion += OnPlaylistViewDragMotion;
			playlistView.DragDrop += OnPlaylistViewDragDrop;	
				
			sourceView.SelectLibrary();
				
			Gtk.Drag.SourceSet(playlistView, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				playlistViewSourceEntries, 
				DragAction.Copy | DragAction.Move);
		
			Gtk.Drag.DestSet(playlistView, 
				DestDefaults.All, playlistViewDestEntries, 
				DragAction.Copy | DragAction.Move);
			
			// Ipod Container
			HBox box = new HBox();
			box.Spacing = 5;
			(gxml["IpodContainer"] as Container).Add(box);
			ipodDiskUsageBar = new ProgressBar();
			box.PackStart(ipodDiskUsageBar, false, false, 0);
			
			Button ipodPropertiesButton = new Button(
				new Gtk.Image("gtk-properties", IconSize.Menu));
			ipodPropertiesButton.Clicked += OnIpodPropertiesClicked;
			box.PackStart(ipodPropertiesButton, false, false, 0);
			
			Button ipodEjectButton = new Button(new Gtk.Image("media-eject",
				IconSize.Menu));
			ipodEjectButton.Clicked += OnIpodEjectClicked;
			box.PackStart(ipodEjectButton, false, false, 0);
			
			box.ShowAll();
			
			// Misc
			SetInfoLabel("Idle");

			// Window Events
			WindowPlayer.KeyPressEvent += 
				new KeyPressEventHandler(OnKeyPressEvent);
			WindowPlayer.ConfigureEvent +=
				new ConfigureEventHandler(OnWindowPlayerConfigureEvent);
			
			// Search Entry
			ArrayList fields = new ArrayList();
			fields.Add("All");
			fields.Add("-");
			fields.Add("Song Name");
			fields.Add("Artist Name");
			fields.Add("Album Title");
			
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
			SetTip(gxml["ButtonPrevious"], Catalog.GetString("Play Previous Song"));
			SetTip(gxml["ButtonPlayPause"], Catalog.GetString("Play/Pause Current Song"));
			SetTip(gxml["ButtonNext"], Catalog.GetString("Play Next Song"));
			SetTip(gxml["ScaleTime"], Catalog.GetString("Current Position in Song"));
			SetTip(volumeButton, Catalog.GetString("Adjust Volume"));
			SetTip(ipodDiskUsageBar, Catalog.GetString("iPod Disk Usage"));
			
			playlistMenuMap = new Hashtable();
			
			Core.Instance.DBusServer.RegisterObject(
				new BansheeCore(Window), "/org/gnome/Banshee/Core");
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
				
				trayIcon.PlayItem.Activated += 
					new EventHandler(OnButtonPlayPauseClicked);
				trayIcon.NextItem.Activated +=
					new EventHandler(OnButtonNextClicked);
				trayIcon.PreviousItem.Activated +=
					new EventHandler(OnButtonPreviousClicked);
				trayIcon.ShuffleItem.Activated += 
					new EventHandler(OnTrayMenuItemShuffleActivated);
				trayIcon.RepeatItem.Activated += 
					new EventHandler(OnTrayMenuItemRepeatActivated);
				trayIcon.ExitItem.Activated +=
					new EventHandler(OnMenuQuitActivate);
			} catch(Exception) {
				trayIcon = null;
				DebugLog.Add(Catalog.GetString(
					"Notification Area Icon could not be installed"));
			}
		}
	
		private void LoadSettings()
		{	
			try {
				volumeButton.Volume = (int)Core.GconfClient.Get
					(GConfKeys.Volume);
			} catch(GConf.NoSuchKeyException) {
				volumeButton.Volume = 80;
			}
			
			Core.Instance.Player.Volume = (ushort)volumeButton.Volume;
			
			try {
				((ToggleButton)gxml["ToggleButtonShuffle"]).Active = 
					(bool)Core.GconfClient.Get
						(GConfKeys.PlaylistShuffle);
				((ToggleButton)gxml["ToggleButtonRepeat"]).Active = 
					(bool)Core.GconfClient.Get
						(GConfKeys.PlaylistRepeat);
			} catch(GConf.NoSuchKeyException) {
				// Default, set in glade file
			}
			
			try {
				SourceSplitter.Position = (int)Core.GconfClient.Get(
					GConfKeys.SourceViewWidth);
			} catch(GConf.NoSuchKeyException) {
				SourceSplitter.Position = 125;
			}
		}
		
		private void PromptForImport()
		{
			Gdk.Threads.Enter();
			
			HigMessageDialog md = new HigMessageDialog(WindowPlayer, 
				DialogFlags.DestroyWithParent, MessageType.Question,
				Catalog.GetString("Import Music"),
				Catalog.GetString("Your music library is empty. You may import new music into " +
				"your library now, or choose to do so later.\n\nAutomatic import " +
				"or importing a large folder may take a long time, so please " +
				"be patient."),
				Catalog.GetString("Import Folder"));
				
			md.AddButton(Catalog.GetString("Automatic Import"), 
				Gtk.ResponseType.Apply, true);
			
			switch(md.Run()) {
				case (int)ResponseType.Ok:
					ImportWithFileSelector();
					break;
				case (int)ResponseType.Apply:
					ImportHomeDirectory();
					break;
			}
			
			md.Destroy();
			Gdk.Threads.Leave();
		}
		
		private void ConnectToLibraryTransactionManager()
		{
			Core.Library.TransactionManager.ExecutionStackChanged +=
				OnLTMExecutionStackChanged;
			Core.Library.TransactionManager.ExecutionStackEmpty +=
				OnLTMExecutionStackEmpty;
		}
		
		private void OnLTMExecutionStackChanged(object o, EventArgs args)
		{	
			if(libraryTransactionStatus == null) {
				libraryTransactionStatus = new LibraryTransactionStatus();
				libraryTransactionStatus.Stopped += 
					OnLibraryTransactionStatusStopped;
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
				sourceView.SelectLibraryForce();
			}
		}
		
		private void LoadSourceView()
		{		
			sourceView.Sensitive = true;
			((Gtk.ScrolledWindow)gxml["SourceContainer"]).Remove(sourceViewLoadingVP);
			((Gtk.ScrolledWindow)gxml["SourceContainer"]).Add(sourceView);
			sourceView.Show();
		}
		
		private void OnLibraryTransactionStatusStopped(object o, 
			EventArgs args)
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
			if(Core.Library.Tracks.Count <= 0) {
				GLib.Timeout.Add(500, PromptForImportTimeout);
			} else {
				startupLoadReady = true;
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
      		playlistView.Shutdown();
			Core.Instance.Player.Dispose();
			Core.GconfClient.Set(GConfKeys.SourceViewWidth, 
				SourceSplitter.Position);
			Core.Instance.Shutdown();
			Application.Quit();
      	}
      
     	private void SetInfoLabel(string text)
      	{
      		LabelInfo.Markup = "<span size=\"small\">" + text + "</span>";
      	}
      	
      	private void TogglePlaying()
		{		
			if(Core.Instance.Player.Playing) {
				ImagePlayPause.SetFromStock("media-play", 
					IconSize.LargeToolbar);
				Core.Instance.Player.Pause();
				if(trayIcon != null) {
					((Gtk.Image)trayIcon.PlayItem.Image).
						SetFromStock("media-play", IconSize.Menu);
				}
			} else {
				ImagePlayPause.SetFromStock("media-pause", 
					IconSize.LargeToolbar);
				Core.Instance.Player.Play();
				if(trayIcon != null) {
					((Gtk.Image)trayIcon.PlayItem.Image).
						SetFromStock("media-pause", IconSize.Menu);
				}
			}
		}
		
		private void UpdateMetaDisplay(TrackInfo ti)
		{
			trackInfoHeader.Artist = ti.DisplayArtist;
			trackInfoHeader.Title = ti.DisplayTitle;
			
			if(trayIcon != null)
				trayIcon.Tooltip = ti.DisplayArtist + " - " + ti.DisplayTitle;
		}
		
		public void PlayFile(TrackInfo ti)
		{
			activeTrackInfo = ti;
			Core.Instance.Player.Close();
			Core.Instance.Player.Open(ti);

			ScaleTime.Adjustment.Lower = 0;
			ScaleTime.Adjustment.Upper = ti.Duration;
			
			UpdateMetaDisplay(ti);
			
			TogglePlaying();
			
			ti.IncrementPlayCount();
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
			if((WindowPlayer.GdkWindow.State & Gdk.WindowState.Maximized) > 0)
				return;

			WindowPlayer.GetPosition(out x, out y);
			WindowPlayer.GetSize(out width, out height);
			
			// might consider putting this in some kind of time delay queue
			// so we're not writing to the gconf client every pixel change
			Core.GconfClient.Set(GConfKeys.WindowX, x);
			Core.GconfClient.Set(GConfKeys.WindowY, y);
			Core.GconfClient.Set(GConfKeys.WindowWidth, width);
			Core.GconfClient.Set(GConfKeys.WindowHeight, height);
		}
		
		private void OnKeyPressEvent(object o, KeyPressEventArgs args)
		{
			switch(args.Event.Key) {
				case Gdk.Key.J:
				case Gdk.Key.j:
				case Gdk.Key.F3:
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
			if(Core.Instance.Player.Loaded)
				TogglePlaying();
			else
				playlistView.PlaySelected();
		}
		
		private void OnButtonPreviousClicked(object o, EventArgs args)
		{
			playlistModel.Regress();
			playlistView.UpdateView();
		}
		
		private void OnButtonNextClicked(object o, EventArgs args)
		{
			playlistModel.Advance();
			playlistView.UpdateView();
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
		
		private void OnMenuSearchBarActivate(object o, EventArgs args)
		{

		}
				
		private void OnMenuPreferencesActivate(object o, EventArgs args)
		{
			new PreferencesWindow();
		}

		// ---- Player Event Handlers ----
		
		private void SetPositionLabel(long position)
		{
			if(activeTrackInfo == null)
				return;
			
			SetInfoLabel(String.Format("{0}:{1:00} of {2}:{3:00}",
				position / 60,
				position % 60,
				activeTrackInfo.Duration / 60,
				activeTrackInfo.Duration % 60)
			);	
		}
		
		private void OnPlayerTick(object o, PlayerEngineIterateArgs args)
		{
			if(activeTrackInfo == null)
				return;
				
			if(Core.Instance.Player.Length > 0 
				&& activeTrackInfo.Duration <= 0) {
				activeTrackInfo.Duration = Core.Instance.Player.Length;
				activeTrackInfo.Save();
				Core.ThreadEnter();
				playlistView.QueueDraw();
				Core.ThreadLeave();
				ScaleTime.Adjustment.Upper = activeTrackInfo.Duration;
			}
				
			if(updateEnginePosition) {
				if(setPositionTimeoutId > 0)
                	GLib.Source.Remove(setPositionTimeoutId);
				setPositionTimeoutId = GLib.Timeout.Add(100,
                	new GLib.TimeoutHandler(SetPositionTimeoutCallback));
			
				Core.ThreadEnter();
				SetPositionLabel(args.Position);
				Core.ThreadLeave();
			}
		}
		
		private bool SetPositionTimeoutCallback()
		{
			setPositionTimeoutId = 0;
			Core.ThreadEnter();
			ScaleTime.Value = Core.Instance.Player.Position;
			Core.ThreadLeave();
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
			Core.ThreadEnter();
			
			StopPlaying();
			
			playlistModel.Continue();
			playlistView.UpdateView();
			
			Core.ThreadLeave();
		}
		
		// ---- Playlist Event Handlers ----

		private void ImportWithFileSelector()
		{
			FileChooserDialog chooser = new FileChooserDialog(
				Catalog.GetString("Import Folder to Library"),
				null,
				FileChooserAction.SelectFolder,
				"gnome-vfs"
			);
			
			chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
			chooser.AddButton(Stock.Open, ResponseType.Ok);
			chooser.DefaultResponse = ResponseType.Ok;
			
			if(chooser.Run() == (int)ResponseType.Ok) 
				playlistModel.AddFile(chooser.CurrentFolderUri);
			
			chooser.Destroy();
		}
		
		private void ImportHomeDirectory()
		{
			playlistModel.AddFile(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
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
				FileChooserAction.Open,
				"gnome-vfs"
			);
			
			chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
			chooser.AddButton(Stock.Open, ResponseType.Ok);
			
			chooser.SelectMultiple = true;
			chooser.DefaultResponse = ResponseType.Ok;
			
			if(chooser.Run() == (int)ResponseType.Ok) {
				foreach(string path in chooser.Uris)
					playlistModel.AddFile(path);
			}
			
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
		
		private void OnPlaylistSaved(object o, EventArgs args)
		{	
			sourceView.RefreshList();
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
			Source source = sourceView.SelectedSource;
			if(source == null)
				return;
				
			searchEntry.CancelSearch(false);
				
			if(source.Type == SourceType.Library) {
				playlistModel.LoadFromLibrary();
				playlistModel.Source = source;
				
				Core.ThreadEnter();
				(gxml["ViewNameLabel"] as Label).Markup = 
					String.Format(Catalog.GetString("<b>{0} Music Library</b>"),
						Core.Instance.UserFirstName);
				
				Core.ThreadLeave();
			} else if(source.Type == SourceType.Ipod) {
				playlistModel.Clear();
				playlistModel.Source = source;
				
				IpodSource ipodSource = source as IpodSource;
				IPod.Device device = ipodSource.Device;
				playlistModel.LoadFromIpodSource(ipodSource);
				
				ipodDiskUsageBar.Fraction = (double)device.VolumeUsed / 
					(double)device.VolumeSize;
				ulong usedmb = device.VolumeUsed / (1024 * 1024);
				ulong availmb = device.VolumeAvailable / (1024 * 1024);
				ulong totalmb = device.VolumeSize / (1024 * 1024);
				
				string usedstr = usedmb >= 1024 ? (usedmb / 1024) + " GB" :
					usedmb + " MB";
				string availstr = availmb >= 1024 ? (availmb / 1024) + " GB" :
					availmb + " MB";
				string totalstr = totalmb >= 1024 ? (totalmb / 1024) + " GB" :
					totalmb + " MB";
				
				ipodDiskUsageBar.Text = usedstr + " of " + totalstr;
				string tooltip = ipodDiskUsageBar.Text + " (" + availstr + 
					" " + Catalog.GetString("Remaining") + ")";
				toolTips.SetTip(ipodDiskUsageBar, tooltip, tooltip);
				
				Core.ThreadEnter();
				(gxml["ViewNameLabel"] as Label).Markup = 
					"<b>" + source.Name + "</b>";
				Core.ThreadLeave();
			} else {
				playlistModel.LoadFromPlaylist(source.Name);
				playlistModel.Source = source;
				
				Core.ThreadEnter();
				(gxml["ViewNameLabel"] as Label).Markup = 
					"<b>" + source.Name + "</b>";
				Core.ThreadLeave();
			}
			
			gxml["IpodContainer"].Visible = source.Type == SourceType.Ipod;
		}
		
		private void OnIpodPropertiesClicked(object o, EventArgs args)
		{
			if(sourceView.SelectedSource.Type != SourceType.Ipod)
				return;
			
			ShowSourceProperties(sourceView.SelectedSource);
		}
		
		private void OnIpodEjectClicked(object o, EventArgs args)
		{
			EjectSource(sourceView.SelectedSource);
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
		
			string timeDisp;
			
			if(d > 0)
				timeDisp = String.Format("{0} day{1}, {2}:{3}:{4}",
					d, d == 1 ? "" : "s", h, m.ToString("00"), s.ToString("00"));
			else if(h > 0)
				timeDisp = String.Format("{0}:{1}:{2}",
					h, m.ToString("00"), s.ToString("00"));
			else	
				timeDisp = String.Format("{0}:{1}",
					m, s.ToString("00"));
		
			if(!Core.Instance.MainThread.Equals(Thread.CurrentThread))
				Gdk.Threads.Enter();
			
			if(count == 0 && playlistModel.Source == null) {
				LabelStatusBar.Text = Catalog.GetString("Banshee Music Player");
			} else if(count == 0) {
				switch(playlistModel.Source.Type) {
					case SourceType.Library:
						LabelStatusBar.Text = Catalog.GetString("Your Library is Empty - Consider Importing Music");
						break;
					case SourceType.Playlist:
						LabelStatusBar.Text = Catalog.GetString("This Playlist is Empty - Consider Adding Music");
						break;
				}
			} else
				LabelStatusBar.Text = String.Format(
					Catalog.GetString("{0} Items, {1} Total Play Time") + " [{2}]",
					count, timeDisp, playlistModel.TotalDuration);
				
			if(!Core.Instance.MainThread.Equals(Thread.CurrentThread))
				Gdk.Threads.Leave();
		}
	
		// PlaylistMenu Handlers
	
		private void OnItemColumnsActivate(object o, EventArgs args)
		{
			playlistView.ColumnChooser();
		}
		
		private void OnItemRemoveActivate(object o, EventArgs args)
		{
			int selCount = playlistView.Selection.CountSelectedRows();
		
			if(selCount <= 0)
				return;
		
			if(playlistModel.Source.Type == SourceType.Library) {
				HigMessageDialog md = new HigMessageDialog(WindowPlayer, 
					DialogFlags.DestroyWithParent, MessageType.Warning,
					ButtonsType.YesNo,
					Catalog.GetString("Remove Selected Songs from Library"),
					String.Format(Catalog.GetString(
					"Are you sure you want to remove the selected <b>({0})</b> song(s) from your library?"), selCount)
				);
				
				if(md.Run() != (int)ResponseType.Yes) {
					md.Destroy();
					return;
				}
		
				md.Destroy();
			}
		
			TreeIter [] iters = new TreeIter[selCount];
			int i = 0;
			
			foreach(TreePath path in playlistView.Selection.GetSelectedRows())			
				playlistModel.GetIter(out iters[i++], path);
		
			TrackRemoveTransaction transaction;
			
			if(playlistModel.Source.Type == SourceType.Library)
				transaction = new LibraryTrackRemoveTransaction();
			else
				transaction = new PlaylistTrackRemoveTransaction(
					Playlist.GetId(playlistModel.Source.Name));
				
			for(i = 0; i < iters.Length; i++) {
				TrackInfo ti = playlistModel.IterTrackInfo(iters[i]);
				playlistModel.RemoveTrack(ref iters[i]);
				transaction.RemoveQueue.Add(ti);
			}
			
			Core.Library.TransactionManager.Register(transaction);
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
			MenuItem sourceDelete = gxmlSourceMenu["ItemSourceDelete"] as MenuItem;
			MenuItem sourceProperties = gxmlSourceMenu["ItemSourceProperties"] as MenuItem;
			ImageMenuItem ejectItem = gxmlSourceMenu["ItemEject"] as ImageMenuItem;
			
			addSelectedSongs.Sensitive = source.Type == SourceType.Playlist
				&& playlistView.Selection.CountSelectedRows() > 0;
			sourceDuplicate.Sensitive = false;
			
			if(source.Type == SourceType.Ipod)
				sourceProperties.Sensitive = true;
			else
				sourceProperties.Sensitive = false;
		
			if(source.CanEject) {
				ejectItem.Image = new Gtk.Image("media-eject", IconSize.Menu);
			}
			
			menu.Popup(null, null, null, IntPtr.Zero, 0, args.Event.Time);
			menu.Show();
			
			sourceDuplicate.Visible = !source.CanEject;
			ejectItem.Visible = source.CanEject;
			sourceDelete.Visible = !source.CanEject;
			
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
			sourceView.RefreshList();
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
			if(sourceView.HighlightedSource == null || 
				sourceView.HighlightedSource.Type == SourceType.Library)
				return;
				
			InputDialog input = new InputDialog(
				Catalog.GetString("Rename Playlist"),
				Catalog.GetString("Enter new playlist name"), 
				"playlist-icon-large.png", 
				sourceView.HighlightedSource.Name);
			string newName = input.Execute();
			if(newName != null)
				sourceView.HighlightedSource.Name = newName;
			sourceView.QueueDraw();
		}
		
		private void OnItemRenamePlaylistActivate(object o, EventArgs args)
		{
			OnItemSourceRenameActivate(o, args);
		}
		
		private void OnItemRemoveSongsActivate(object o, EventArgs args)
		{
			OnItemRemoveActivate(o, args);
		}
		
		private void OnItemDeletePlaylistActivate(object o, EventArgs args)
		{
			OnItemSourceDeleteActivate(o, args);
		}
		
		private void OnItemSelectAllActivate(object o, EventArgs args)
		{
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
				IPod.Device device = (source as IpodSource).Device;
				collection = device.SongDatabase.Songs;
				
				// TODO: this sucks! Song->IpodTrackInfo needs to be cached!
				ArrayList tmpCol = new ArrayList();
				foreach(IPod.Song ti in collection)
					tmpCol.Add(new IpodTrackInfo(ti));
				collection = tmpCol;
			} else {
				collection = Core.Library.Tracks.Values;
			}
			
			foreach(TrackInfo ti in collection) {
				string match;
				
				try {
					switch(field) {
						case "Artist Name":
							match = ti.Artist;
							break;
						case "Song Name":
							match = ti.Title;
							break;
						case "Album Title":
							match = ti.Album;
							break;
						case "All":
						default:
							string [] matches = {
								ti.Artist,
								ti.Album,
								ti.Title
							};
							
							foreach(string m in matches) {
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
			if(args.Event.Button == 3) {
				//GLib.Timeout.Add(10, 
				//	new GLib.TimeoutHandler(PlaylistMenuPopupTimeout));
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
			    	if(playlistView.Selection.PathIsSelected(path)) 
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
			if(!Gtk.Drag.CheckThreshold(playlistView, clickX, clickY,
						    (int)args.Event.X, (int)args.Event.Y))
				return;
		
			Gtk.Drag.Begin(playlistView, new TargetList (playlistViewSourceEntries),
				       Gdk.DragAction.Move | Gdk.DragAction.Copy, 1, args.Event);
			args.RetVal = true;
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
			
			menu.Popup(null, null, null, IntPtr.Zero, 0, time);
			
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
			pl.Saved += OnPlaylistSavedRefreshSourceView;
		}

		private void OnPlaylistViewDragMotion(object o, DragMotionArgs args)
		{
			TreePath path;
			TreeViewDropPosition pos;

			if(!playlistView.GetDestRowAtPos(args.X, args.Y, out path, out pos))
				return;
			playlistView.SetDragDestRow(path, (TreeViewDropPosition)((int)pos & 0x1));
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
						playlistModel.AddFile(rawSelectionData);
						
					break;
				case (uint)Dnd.TargetType.PlaylistViewModel:
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
							destIter = iter.Copy();
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
				case (uint)Dnd.TargetType.PlaylistViewModel:				
					selData = Dnd.TreeViewSelectionPathsToBytes(playlistView);
					if(selData == null)
						return;
					
					args.SelectionData.Set(
						Gdk.Atom.Intern(Dnd.TargetPlaylist.Target, 
						false), 8, selData);
						
					break;
				case (uint)Dnd.TargetType.SourceViewModel:
					selData = Dnd.PlaylistSelectionTrackIdsToBytes(playlistView);
					if(selData == null)
						return;
					
					args.SelectionData.Set(
						Gdk.Atom.Intern(Dnd.TargetSource.Target,
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
		
		// Source View DnD

		private void OnSourceViewDragMotion(object o, DragMotionArgs args)
		{
			TreePath path;
			Source source;
			
			if(!sourceView.GetPathAtPos(args.X, args.Y, out path))
				return;
				
			source = sourceView.GetSource(path);
			if(source == null)
				return;
				
			switch(source.Type) {
				case SourceType.Library:
					return;
			}
				
			sourceView.SetDragDestRow(path, 
				Gtk.TreeViewDropPosition.IntoOrAfter);
		}
		
		private void OnSourceViewDragDataReceived(object o, 
			DragDataReceivedArgs args)
		{
			TreePath destPath;
			TreeViewDropPosition pos;
			bool haveDropPosition;
			
			string rawData = Dnd.SelectionDataToString(args.SelectionData);		
			string [] rawDataArray = Dnd.SplitSelectionData(rawData);
			if(rawData.Length <= 0) 
				return;		
			
			haveDropPosition = sourceView.GetDestRowAtPos(args.X, args.Y, 
				out destPath, out pos);

			switch(args.Info) {
				case (uint)Dnd.TargetType.SourceViewModel: // makes no sense!
					ArrayList tracks = new ArrayList();
					foreach(string trackId in rawDataArray) {
						try {
							int tid = Convert.ToInt32(trackId);
							tracks.Add(Core.Library.Tracks[tid]);
						} catch(Exception) {
							continue;
						}
					}
					
					Source source = sourceView.GetSource(destPath);
					
					if(source == null) {
						Playlist pl = new Playlist(Playlist.GoodUniqueName(tracks));
						pl.Append(tracks);
						pl.Save();
						pl.Saved += OnPlaylistSavedRefreshSourceView;
					} else if(haveDropPosition
						&& source.Type == SourceType.Playlist) {
						Playlist pl = new Playlist(source.Name);
						pl.Load();
						pl.Append(tracks);
						pl.Save();
					}
					
					break;
				case (uint)Dnd.TargetType.UriList:
					// M3U URI Drops Here?
					break;
			}
			
			Gtk.Drag.Finish(args.Context, true, false, args.Time);
		}
		
		private void OnPlaylistSavedRefreshSourceView(object o, EventArgs args)
		{
			sourceView.RefreshList();
		}
		
		private void OnButtonBurnClicked(object o, EventArgs args)
		{
			if(playlistView.Selection.CountSelectedRows() <= 0)
				return;
		
			BurnCore burnCore = new BurnCore(BurnCore.DiskType.Audio);
		
			foreach(TreePath path in playlistView.Selection.GetSelectedRows())
				burnCore.AddTrack(playlistModel.PathTrackInfo(path));
				
			burnCore.Burn();
		}
		
		private void EjectSource(Source source)
		{
			if(source.CanEject) {
				try {
					if(source.GetType() == typeof(IpodSource)) {
						if(activeTrackInfo.GetType() == typeof(IpodTrackInfo)) {
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
						new IpodPropertiesDialog(device);
					propWin.Run();
					propWin.Destroy();
					if(propWin.Edited && device.CanWrite)
						device.Save();
					source.Name = device.Name;
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
			SetInfoLabel("Idle");
			trackInfoHeader.SetIdle();
			activeTrackInfo = null;
			
			if(trayIcon != null)
				trayIcon.Tooltip = Catalog.GetString("Banshee - Idle");
		}
	}
}
