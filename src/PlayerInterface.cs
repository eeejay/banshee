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
using Glade;

using Sql;

namespace Sonance
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
		
		private long plLoaderMax, plLoaderCount;

        public PlayerUI() 
        {
			gxml = new Glade.XML(null, "player.glade", "WindowPlayer", null);
			gxml.Autoconnect(this);

			ResizeMoveWindow();
			BuildWindow();   
			InstallTrayIcon();
			InstallMmKeys();
			
			Core.Instance.Player.Tick += new TickEventHandler(OnPlayerTick);
			Core.Instance.Player.Eos += new EventHandler(OnPlayerEos);		
			
			LoadSettings();
			Core.Instance.PlayerInterface = this;
			
			GLib.Timeout.Add(500, InitialLoadTimeout);
			
			/*Gdk.Threads.Enter();
			HigMessageDialog wd = new HigMessageDialog(WindowPlayer,
				DialogFlags.DestroyWithParent, MessageType.Warning, ButtonsType.Ok,
				"Unstable and Incomplete",
				"Sonance is currently under heavy development. As such, many " +
				"features are incomplete, broken, or unimplemented. Do not " +
				"expect a working audio management and playback platform " + 
				"with this release. Consider for testing and development only.");
			wd.Run();
			wd.Destroy();
			Gdk.Threads.Leave();*/
			
			
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
      		WindowPlayer.Icon = Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			
			ImagePrevious.SetFromStock("media-prev", IconSize.LargeToolbar);
			ImageNext.SetFromStock("media-next", IconSize.LargeToolbar);
			ImagePlayPause.SetFromStock("media-play", IconSize.LargeToolbar);
				
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
			sourceView = new SourceView();
			((Gtk.ScrolledWindow)gxml["SourceContainer"]).Add(sourceView);
			sourceView.Show();
			sourceView.SourceChanged += OnSourceChanged;
			sourceView.ButtonPressEvent += OnSourceViewButtonPressEvent;

			// Playlist View
			playlistModel = new PlaylistModel();
			playlistView = new PlaylistView(playlistModel);
			((Gtk.ScrolledWindow)gxml["LibraryContainer"]).Add(playlistView);
			playlistView.Show();
			playlistModel.Updated += OnPlaylistUpdated;
			playlistView.ButtonPressEvent += OnPlaylistViewButtonPressEvent;
			
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
			
			SearchEntry searchEntry = new SearchEntry(fields);
			searchEntry.Show();
			((HBox)gxml["PlaylistHeaderBox"]).PackStart(searchEntry, 
				false, false, 0);
      	}
      	
      	private void InstallTrayIcon()
      	{
      		try {
				trayIcon = new NotificationAreaIcon();
				trayIcon.ClickEvent += new EventHandler(OnTrayClick);
				trayIcon.ScrollEvent += new ScrollEventHandler(OnTrayScroll);
				
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
				DebugLog.Add("egg-tray could not be installed [no libsonance]");
			}
		}
		
		private void InstallMmKeys()
		{
			/*try {
				MmKeys mmkeys = new MmKeys();
				mmkeys.TogglePlay += new EventHandler(OnButtonPlayPauseClicked);
				mmkeys.Next += new EventHandler(OnButtonNextClicked);
				mmkeys.Previous += new EventHandler(OnButtonPreviousClicked);
			} catch(Exception) {
				DebugLog.Add("mm-keys could not be installed [no libsonance]");
			}*/
		}
		
		private void LoadSettings()
		{	
			try {
				volumeButton.Volume = (int)Core.GconfClient.Get
					(GConfKeys.Volume);
			} catch(GConf.NoSuchKeyException) {
				volumeButton.Volume = 80;
			}
			
			Core.Instance.Player.Volume = (double)volumeButton.Volume;
			
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
				"Import Music",
				"Your music library is empty. You may import new music into " +
				"your library now, or choose to do so later.",
				"Import Music");
			
			if(md.Run() == (int)ResponseType.Ok)
				ImportWithFileSelector();
			
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
					
				Console.WriteLine("LTM NEW");
			}
			
			if(libraryTransactionStatus.AllowShow) {
				headerNotebook.AddPage(libraryTransactionStatus, true);	
				libraryTransactionStatus.Start();
			}
		}
		
		private void OnLTMExecutionStackEmpty(object o, EventArgs args)
		{
			DebugLog.Add("LTMExecutionStackEmpty");
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
			if(Core.Library.Tracks.Count <= 0)
				GLib.Timeout.Add(500, PromptForImportTimeout);
			/*else {
				TreeIter iter;
				sourceView.Model.GetIterFirst(out iter);
				sourceView.ActivateRow(sourceView.Model.GetPath(iter), 
					sourceView.Columns[0]);
			}*/
		}
		
		private bool PromptForImportTimeout()
		{
			PromptForImport();
			
			return false;
		}
		
		// ---- Misc. Utility Routines ----
      
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
					((Image)trayIcon.PlayItem.Image).
						SetFromStock("media-play", IconSize.Menu);
				}
			} else {
				ImagePlayPause.SetFromStock("media-pause", 
					IconSize.LargeToolbar);
				Core.Instance.Player.Play();
				if(trayIcon != null) {
					((Image)trayIcon.PlayItem.Image).
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
		}

		// ---- Window Event Handlers ----
		/*private TrackInfo IterTrackInfo(TreeIter iter)
		{
			return GetValue(iter, 0) as TrackInfo;
		}*/

		private void OnWindowPlayerDeleteEvent(object o, DeleteEventArgs args) 
		{
			playlistView.Shutdown();
			Core.GconfClient.Set(GConfKeys.SourceViewWidth, 
				SourceSplitter.Position);
			Core.Instance.Shutdown();
			Application.Quit();
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
			/*if(Core.Instance.Player.Loaded)
				TogglePlaying();
			else
				playlistView.PlaySelected();*/
		}
		
		private void OnButtonPreviousClicked(object o, EventArgs args)
		{
			playlistModel.Regress();
			playlistView.QueueDraw();
		}
		
		private void OnButtonNextClicked(object o, EventArgs args)
		{
			playlistModel.Advance();
			playlistView.QueueDraw();
		}
		
		private void OnVolumeScaleChanged(int volume)
		{
			Core.Instance.Player.Volume = (double)volume;
			Core.GconfClient.Set(GConfKeys.Volume, volume);
		}
		
		// ---- Main Menu Event Handlers ----
		
		private void OnMenuQuitActivate(object o, EventArgs args)
		{
			Core.Instance.Shutdown();
			Application.Quit();
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
		
		private void OnPlayerTick(object o, TickEventArgs args)
		{
			if(activeTrackInfo == null)
				return;
				
			ScaleTime.Value = args.Position;
			SetInfoLabel(String.Format("{0}:{1:00} of {2}:{3:00}",
				args.Position / 60,
				args.Position % 60,
				activeTrackInfo.Duration / 60,
				activeTrackInfo.Duration % 60)
			);	
		}
		
		private void OnPlayerEos(object o, EventArgs args)
		{
			DebugLog.Add("OnPlayerEos entry");
			
			if(!Core.Instance.MainThread.Equals(Thread.CurrentThread))
				Gdk.Threads.Enter();
			
			ImagePlayPause.SetFromStock("media-play", IconSize.LargeToolbar);
			ScaleTime.Adjustment.Lower = 0;
			ScaleTime.Adjustment.Upper = 0;
			ScaleTime.Value = 0;
			SetInfoLabel("Idle");
			activeTrackInfo = null;
			
			if(trayIcon != null)
				trayIcon.Tooltip = "Sonance - Idle";
			
			if(!Core.Instance.MainThread.Equals(Thread.CurrentThread))
				Gdk.Threads.Leave();
			
			playlistModel.Continue();
			playlistView.QueueDraw();
		}
		
		// ---- Playlist Event Handlers ----

		private void ImportWithFileSelector()
		{
			FileChooserDialog chooser = new FileChooserDialog(
				"Import Folder to Library",
				null,
				FileChooserAction.SelectFolder,
				"gnome-vfs"
			);
			
			chooser.AddButton(Stock.Open, ResponseType.Ok);
			chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
			chooser.DefaultResponse = ResponseType.Ok;
			
			if(chooser.Run() == (int)ResponseType.Ok) 
				playlistModel.AddFile(chooser.CurrentFolderUri);
			
			chooser.Destroy();
		}

		private void OnMenuImportFolderActivate(object o, EventArgs args)
		{
			ImportWithFileSelector();
		}
		
		private void OnMenuImportFilesActivate(object o, EventArgs args)
		{
			FileChooserDialog chooser = new FileChooserDialog(
				"Import Files to Library",
				null,
				FileChooserAction.Open,
				"gnome-vfs"
			);
			
			chooser.AddButton(Stock.Open, ResponseType.Ok);
			chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
			chooser.SelectMultiple = true;
			chooser.DefaultResponse = ResponseType.Ok;
			
			if(chooser.Run() == (int)ResponseType.Ok) {
				foreach(string path in chooser.Uris)
					playlistModel.AddFile(path);
			}
			
			chooser.Destroy();
		}
		
		private void OnMenuPlaylistClearActivate(object o, EventArgs args)
		{
			//playlistView.Clear();
		}
				
		private void OnMenuTrackPropertiesActivate(object o, EventArgs args)
		{
			//new TrackProperties(playlistView.SelectedTrackInfo);
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
		
		}
		
		private void OnMenuPlaylistRemoveActivate(object o, EventArgs args)
		{
			new SqlBuilderUI();
		}
		
		private void OnMenuPlaylistPropertiesActivate(object o, EventArgs args)
		{
		
		}
		
		/*private void OnButtonRemoveClicked(object o, EventArgs args)
		{
			playlistView.RemoveSelected();
		}*/
		
		/*private void OnButtonMoveUpClicked(object o, EventArgs args)
		{
			playlistView.MoveUp();
		}
		
		private void OnButtonMoveDownClicked(object o, EventArgs args)
		{
			playlistView.MoveDown();
		}*/
		
		/*private void OnButtonSaveClicked(object o, EventArgs args)
		{
			string name = InputDialog.Run(
				"Save Playlist", "Playlist Name:", "playlist-icon-large.png", 
				playlistView.PlaylistName);
				
			if(name == null)
				return;
				
			name = name.Trim();
			if(name.Length == 0) {
				SimpleMessageDialogs.Error("You must specify a playlist name");
				return;
			}
			
			if(Sonance.Playlist.Exists(name) && 
				!name.Equals(playlistView.PlaylistName)) {
				if(SimpleMessageDialogs.YesNo("The playlist '" 
					+ name + "' already exists. Overwrite?") 
					!= ResponseType.Yes) {
					return;
				}
			}
				
			playlistView.PlaylistName = name;
			playlistView.Save(name);
			
			sourceView.RefreshList();
		}*/
		
		private void OnSourceChanged(object o, EventArgs args)
		{
			Source source = sourceView.SelectedSource;
			if(source == null)
				return;
				
			Statement query;
			
			if(source.Type == SourceType.Library) {
				playlistModel.LoadFromLibrary();
			} else {
				int id = Playlist.GetId(source.Name);
				query = new Statement(
					"SELECT t.* " + 
					"FROM PlaylistEntries p, Tracks t " + 
					"WHERE t.TrackID = p.TrackID AND p.PlaylistID = " + id);

				//playlistView.Clear();
				Console.WriteLine(query);
				playlistModel.AddSql(query);
			}
		}
		
		private void OnToggleButtonShuffleToggled(object o, EventArgs args)
		{
			ToggleButton t = (ToggleButton)o;
			playlistModel.Shuffle = t.Active;
			Core.GconfClient.Set(GConfKeys.PlaylistShuffle, 
				t.Active);
				
			if(trayIcon != null)
				((Image)trayIcon.ShuffleItem.Image).SetFromStock(
					t.Active ? "gtk-yes" : "gtk-no", IconSize.Menu);
		}
		
		private void OnToggleButtonRepeatToggled(object o, EventArgs args)
		{
			ToggleButton t = (ToggleButton)o;
			playlistModel.Repeat = t.Active;
			Core.GconfClient.Set(GConfKeys.PlaylistRepeat, 
				t.Active);
				
			if(trayIcon != null)
				((Image)trayIcon.RepeatItem.Image).SetFromStock(
					t.Active ? "gtk-yes" : "gtk-no", IconSize.Menu);
		}
		
		private void OnPlaylistUpdated(object o, EventArgs args)
		{
			long h = playlistModel.TotalDuration / 3600;
			long m = (playlistModel.TotalDuration / 60) - (h * 60);
			long s = playlistModel.TotalDuration % 60;
			string timeDisp;
	
			if(h > 0)
				timeDisp = String.Format("{0}:{1}:{2}",
					h, m.ToString("00"), s.ToString("00"));
			else	
				timeDisp = String.Format("{0}:{1}",
					m, s.ToString("00"));
		
			if(!Core.Instance.MainThread.Equals(Thread.CurrentThread))
				Gdk.Threads.Enter();
			
			LabelStatusBar.Text = String.Format(
				"{0} Items, {1} Total Play Time",
				playlistModel.Count(), timeDisp);
				
			if(!Core.Instance.MainThread.Equals(Thread.CurrentThread))
				Gdk.Threads.Leave();
		}
		
		// ---- Search Event Handlers ----

		/*private void OnEntrySearchChanged(object o, EventArgs args)
		{
			string searchString = ((Gtk.Entry)o).Text.Trim();
			int matches = 0;
			bool cleared = true;
			
			if(searchString.Length > 0) {
				matches = playlistView.SearchQuery(searchString);
				cleared = false;
			}
			
			((Gtk.Label)gxml["LabelSearchResults"]).Markup = 
				"<span size=\"small\""
				+ (!cleared && matches == 0 ? " color=\"red\"" : "") +
				">(" + matches + " Hit" + (matches == 1 ? "" : "s") 
				+ ")</span>";
				
			gxml["ButtonSearchBack"].Sensitive = false;
			gxml["ButtonSearchForward"].Sensitive = matches > 1;
		}
		
		private void OnButtonSearchBackClicked(object o, EventArgs args)
		{
			playlistView.SearchBack();
			SensitizeSearchButtons();
		}
		
		private void OnButtonSearchForwardClicked(object o, EventArgs args)
		{
			playlistView.SearchForward();
			SensitizeSearchButtons();
		}
		
		private void OnEntrySearchActivate(object o, EventArgs args)
		{
			if(playlistView.SearchMatches > 0)
				playlistView.PlaySelected();
		}*/
	
		// PlaylistMenu Handlers
	
		[GLib.ConnectBefore]
		private void OnPlaylistViewButtonPressEvent(object o, 
			ButtonPressEventArgs args)
		{			
			if(args.Event.Button != 3)
				return;
				
			if(gxmlPlaylistMenu == null) {
				gxmlPlaylistMenu = new Glade.XML(null, "player.glade", 
					"PlaylistMenu", null);
				gxmlPlaylistMenu.Autoconnect(this);
			}
			
			Menu menu = gxmlPlaylistMenu["PlaylistMenu"] as Menu;
			menu.Popup(null, null, null, IntPtr.Zero, 0, args.Event.Time);
			menu.ShowAll();
		}
		
		private void OnItemColumnsActivate(object o, EventArgs args)
		{
			playlistView.ColumnChooser();
		}
		
		private void OnItemRemoveActivate(object o, EventArgs args)
		{
			//playlistView.RemoveSelected();
		}
		
		private void OnItemPropertiesActivate(object o, EventArgs args)
		{
			//new TrackProperties(playlistView.SelectedTrackInfo);
		}
		
		// SourceMenu Handlers
		
		private uint popupTime;
		
		[GLib.ConnectBefore]
		private void OnSourceViewButtonPressEvent(object o, 
			ButtonPressEventArgs args)
		{
			if(args.Event.Button != 3)
				return;
				
			// Uguugh - the actual selection doesn't take place until *after*
			// we finish here - garh
			popupTime = args.Event.Time;
			GLib.Timeout.Add(1, SourceViewPopupTimeout);
		}
		
		private bool SourceViewPopupTimeout()
		{
			if(sourceView.HighlightedSource == null)
				return false;
				
			SourceType type = sourceView.HighlightedSource.Type;

			if(type == SourceType.Library)
				return false;

			if(gxmlSourceMenu == null) {
				gxmlSourceMenu = new Glade.XML(null, "player.glade", 
					"SourceMenu", null);
				gxmlSourceMenu.Autoconnect(this);
			}
			
			Menu menu = gxmlSourceMenu["SourceMenu"] as Menu;
			(gxmlSourceMenu["ItemAddSelectedSongs"] as MenuItem).Sensitive =
				type  == SourceType.Playlist 
				/*&& playlistView.Selection.CountSelectedRows() > 0*/;
			
			menu.Popup(null, null, null, IntPtr.Zero, 0, popupTime);
			menu.ShowAll();
			
			return false;
		}
		
		private void OnItemAddSelectedSongsActivate(object o, EventArgs args)
		{
			Source source = sourceView.HighlightedSource;
			
			if(source == null || source.Type != SourceType.Playlist)
				return;
				
			//playlistView.AddSelectedToPlayList(source.Name);
		}
		
		private void OnItemSourceDuplicateActivate(object o, EventArgs args)
		{
			Console.WriteLine("OnItemSourceDuplicateActivate");
		}
		
		private void OnItemSourceDeleteActivate(object o, EventArgs args)
		{
			Source source = sourceView.HighlightedSource;
			
			if(source == null || source.Type != SourceType.Playlist)
				return;
				
			Playlist.Delete(source.Name);
			sourceView.RefreshList();
		}
		
		private void OnItemSourcePropertiesActivate(object o, EventArgs args)
		{

		}
	}
}
