/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  TrackProperties.cs
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
using Gtk;
using Glade;
using System.Collections;
using Mono.Posix;

namespace Banshee
{
	internal class EditorTrack
	{
		private TrackInfo track;
		
		public string Artist;
		public string Album;
		public string Title;
		public Uri Uri;
	
		public uint TrackNumber;
		public uint TrackCount;
	
		public EditorTrack(TrackInfo track)
		{
			this.track = track;
			Revert();
		}
		
		public void Revert()
		{
			Artist = track.Artist;
			Album = track.Album;
			Title = track.Title;
			TrackNumber = track.TrackNumber;
			TrackCount = track.TrackCount;
			Uri = track.Uri;
		}
		
		public void Save()
		{
			track.Artist = Artist;
			track.Album = Album;
			track.Title = Title;
			track.TrackNumber = TrackNumber;
			track.TrackCount = TrackCount;
			track.Uri = Uri;
		}
		
		public TrackInfo Track
		{
			get {
				return track;
			}
		}
	}

	public class TrackProperties
	{
		[Widget] private Window WindowTrackInfo;
		[Widget] private Button CancelButton;
		[Widget] private Button SaveButton;
		[Widget] private Button Previous;
		[Widget] private Button Next;
		[Widget] private Button TrackNumberIterator;
		[Widget] private Button TrackNumberSync;
		[Widget] private Button TrackCountSync;
		[Widget] private Button ArtistSync;
		[Widget] private Button AlbumSync;
		[Widget] private Button TitleSync;
		[Widget] private Label TitleLabel;
		[Widget] private SpinButton TrackCount;
		[Widget] private SpinButton TrackNumber;
		[Widget] private Entry Artist;
		[Widget] private Entry Album;
		[Widget] private Entry Title;
		[Widget] private Container EditorContainer;
		
		Tooltips tips = new Tooltips();
		
		private Glade.XML glade;
		
		private ArrayList TrackSet = new ArrayList();
		private int currentIndex = 0;

		public event EventHandler Saved;

		public TrackProperties(TrackInfo [] selection)
		{
			if(selection == null)
				return;
		
			foreach(TrackInfo track in selection)
				TrackSet.Add(new EditorTrack(track));
		
			glade = new Glade.XML(null, 
				"trackinfo.glade", "WindowTrackInfo", null);
			glade.Autoconnect(this);
			WindowTrackInfo.Icon = 
				Gdk.Pixbuf.LoadFromResource("banshee-icon.png");
	
			(glade["BackImage"] as Image).SetFromStock("gtk-go-back", 
				IconSize.Button);
			(glade["ForwardImage"] as Image).SetFromStock("gtk-go-forward", 
				IconSize.Button);
				
			CancelButton.Clicked += OnCancelButtonClicked;
			SaveButton.Clicked += OnSaveButtonClicked;
			Previous.Clicked += OnPreviousClicked;
			Next.Clicked += OnNextClicked;
			
			TrackNumberIterator.Clicked += OnTrackNumberIteratorClicked;
			TrackNumberSync.Clicked += OnTrackNumberSyncClicked;
			TrackCountSync.Clicked += OnTrackCountSyncClicked;
			ArtistSync.Clicked += OnArtistSyncClicked;
			AlbumSync.Clicked += OnAlbumSyncClicked;
			TitleSync.Clicked += OnTitleSyncClicked;
				
			Next.Visible = TrackSet.Count > 1;
			Previous.Visible = TrackSet.Count > 1;
				
			//glade["MultiTrackHeader"].Visible = TrackSet.Count > 1;
			TrackNumberIterator.Visible = TrackSet.Count > 1;
			TrackNumberSync.Visible = TrackSet.Count > 1;
			TrackCountSync.Visible = TrackSet.Count > 1;
			ArtistSync.Visible = TrackSet.Count > 1;
			AlbumSync.Visible = TrackSet.Count > 1;
			TitleSync.Visible = TrackSet.Count > 1;
			
			tips.SetTip(TrackNumberSync, 
				Catalog.GetString("Set all Track Numbers to this value"), 
				"track numbers");
			tips.SetTip(TrackNumberIterator, 
				Catalog.GetString("Automatically Set All Track Numbers"), 
				"track iterator");
			tips.SetTip(TrackCountSync, 
				Catalog.GetString("Set all Track Counts to this value"),
				"track counts");
			tips.SetTip(ArtistSync,
				Catalog.GetString("Set all Artists to this value"), "artists");
			tips.SetTip(AlbumSync, 
				Catalog.GetString("Set all Albums to this value"), "albums");
			tips.SetTip(TitleSync, 
				Catalog.GetString("Set all Titles to this value"), "titles");
				
			LoadTrack(0);
				
			WindowTrackInfo.Show();
		}
		
		private string PrepareStatistic(string stat)
		{
			return "<small><i>" + stat + "</i></small>";
		}
		
		private void LoadTrack(int index)
		{
			if(index < 0 || index >= TrackSet.Count)
				return;
				
			EditorTrack track = TrackSet[index] as EditorTrack;
		
			TrackNumber.Value = track.TrackNumber;
			TrackCount.Value = track.TrackCount;
		
			(glade["Artist"] as Entry).Text = track.Artist;
			(glade["Album"] as Entry).Text = track.Album;
			(glade["Title"] as Entry).Text = track.Title;
			
			(glade["DurationLabel"] as Label).Markup = 
				PrepareStatistic(String.Format("{0}:{1}", 
				track.Track.Duration / 60, 
				(track.Track.Duration % 60).ToString("00")));
			(glade["PlayCountLabel"] as Label).Markup = 
				PrepareStatistic(track.Track.NumberOfPlays.ToString());
	
			(glade["LastPlayedLabel"] as Label).Markup = 
				PrepareStatistic(track.Track.LastPlayed == DateTime.MinValue ?
					Catalog.GetString("Never Played") :
					track.Track.LastPlayed.ToString());
			(glade["ImportedLabel"] as Label).Markup = 
				PrepareStatistic(track.Track.DateAdded == DateTime.MinValue ?
					Catalog.GetString("Unknown") :
					track.Track.DateAdded.ToString());
					
		    if(TrackSet.Count > 1) {
        			TitleLabel.Markup = "<big><b>" +	
        				String.Format(Catalog.GetString(
        					"Editing Track Properties ({0} of {1})"), 
        					index + 1, TrackSet.Count) + "</b></big>";
		    } else {
		          TitleLabel.Markup = "<big><b>" + 
		              Catalog.GetString("Editing Track Properties") 
		              + "</b></big>";    
		    }			
		    
		    tips.SetTip(glade["TitleEventBox"],
				String.Format(Catalog.GetString("File: {0}"), 
				    track.Uri.AbsoluteUri), "uri");
		
			Previous.Sensitive = index > 0;
			Next.Sensitive = index < TrackSet.Count - 1;
		}
		
		private void OnPreviousClicked(object o, EventArgs args)
		{
			UpdateCurrent();
			LoadTrack(--currentIndex);
		}
		
		private void OnNextClicked(object o, EventArgs args)
		{
			UpdateCurrent();
			LoadTrack(++currentIndex);
		}
		
		private void OnTrackNumberSyncClicked(object o, EventArgs args)
		{
			foreach(EditorTrack track in TrackSet)
				track.TrackNumber = (uint)TrackNumber.Value;
		}
		
		private void OnTrackNumberIteratorClicked(object o, EventArgs args)
		{
			int i = 1;
			foreach(EditorTrack track in TrackSet) {
				track.TrackNumber = (uint)i++;
				track.TrackCount = (uint) TrackSet.Count;
			}

			EditorTrack current_track = TrackSet[currentIndex] as EditorTrack;
			TrackNumber.Value = (int) current_track.TrackNumber;
			TrackCount.Value = (int) current_track.TrackCount;
		}
		
		private void OnTrackCountSyncClicked(object o, EventArgs args)
		{
			foreach(EditorTrack track in TrackSet)
				track.TrackCount = (uint)TrackCount.Value;
		}

		private void OnArtistSyncClicked(object o, EventArgs args)
		{
			foreach(EditorTrack track in TrackSet)
				track.Artist = Artist.Text;
		}

		private void OnAlbumSyncClicked(object o, EventArgs args)
		{
			foreach(EditorTrack track in TrackSet)
				track.Album = Album.Text;
		}
		
		private void OnTitleSyncClicked(object o, EventArgs args)
		{
			foreach(EditorTrack track in TrackSet)
				track.Title = Title.Text;
		}
		
		private void UpdateCurrent()
		{
			if(currentIndex < 0 || currentIndex >= TrackSet.Count)
				return;
				
			EditorTrack track = TrackSet[currentIndex] as EditorTrack;
			
			track.TrackNumber = (uint)TrackNumber.Value;
			track.TrackCount = (uint)TrackCount.Value;
			track.Artist = Artist.Text;
			track.Album = Album.Text;
			track.Title = Title.Text;
		}

		private void OnCancelButtonClicked(object o, EventArgs args)
		{
			WindowTrackInfo.Destroy();
		}
		
		private void OnSaveButtonClicked(object o, EventArgs args)
		{
			UpdateCurrent();
			
			ArrayList list = new ArrayList();
			foreach(EditorTrack track in TrackSet) {
				track.Save();
				list.Add(track.Track);
			}
			
			TrackInfoSaveTransaction saveTransaction 
				= new TrackInfoSaveTransaction(list);
			saveTransaction.Register();
				
			EventHandler handler = Saved;
			if(handler != null)
				handler(this, new EventArgs());
				
			WindowTrackInfo.Destroy();
		}
	}
}
