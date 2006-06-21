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
using Mono.Unix;

using Banshee.Base;

namespace Banshee
{
    internal class EditorTrack
    {
        private TrackInfo track;
        
        public string Artist;
        public string Album;
        public string Title;
        public string Genre;
        public SafeUri Uri;
    
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
            Genre = track.Genre;
            TrackNumber = track.TrackNumber;
            TrackCount = track.TrackCount;
            Uri = track.Uri;
        }
        
        public void Save()
        {
            track.Artist = Artist;
            track.Album = Album;
            track.Title = Title;
            track.Genre = Genre;
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
        [Widget] private Button GenreSync;
        [Widget] private Label GenreLabel;
        [Widget] private SpinButton TrackCount;
        [Widget] private SpinButton TrackNumber;
        [Widget] private Entry Artist;
        [Widget] private Entry Album;
        [Widget] private Entry Title;
        [Widget] private ComboBoxEntry Genre;
        [Widget] private Container EditorContainer;
        [Widget] private Expander AdvancedExpander;
        [Widget] private Label Uri;
        [Widget] private Label BitRate;
        [Widget] private Label SampleRate;
        [Widget] private Label Vbr;
        [Widget] private Label Channels;
        [Widget] private Label MimeType;
        [Widget] private Label ExtraInfo;
        
        Tooltips tips = new Tooltips();
        
        private Glade.XML glade;
        
        private ArrayList TrackSet = new ArrayList();
        private int currentIndex = 0;

        public event EventHandler Saved;

        public TrackProperties(TrackInfo [] selection)
        {
            if(selection == null) {
                return;
            }
        
            foreach(TrackInfo track in selection) {
                TrackSet.Add(new EditorTrack(track));
            }
            
            glade = new Glade.XML(null, "banshee.glade", "WindowTrackInfo", null);
            glade.Autoconnect(this);
            IconThemeUtils.SetWindowIcon(WindowTrackInfo);
            
            (glade["BackImage"] as Image).SetFromStock("gtk-go-back", IconSize.Button);
            (glade["ForwardImage"] as Image).SetFromStock("gtk-go-forward", IconSize.Button);
                
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
            GenreSync.Clicked += OnGenreSyncClicked;
            
            Artist.Changed += OnValueEdited;
            Album.Changed += OnValueEdited;
            Title.Changed += OnValueEdited;
            Genre.Entry.Changed += OnValueEdited;
            ListStore genre_model = new ListStore(typeof(string));

            Genre.Model = genre_model;
            Genre.TextColumn = 0;
            
            foreach(string genre in Globals.Library.GetGenreList()) {
                genre_model.AppendValues(genre);            
            }
            
            Next.Visible = TrackSet.Count > 1;
            Previous.Visible = TrackSet.Count > 1;
                
            glade["MultiTrackHeader"].Visible = TrackSet.Count > 1;
            TrackNumberIterator.Visible = TrackSet.Count > 1;
            TrackNumberSync.Visible = TrackSet.Count > 1;
            TrackCountSync.Visible = TrackSet.Count > 1;
            ArtistSync.Visible = TrackSet.Count > 1;
            AlbumSync.Visible = TrackSet.Count > 1;
            TitleSync.Visible = TrackSet.Count > 1;
            GenreSync.Visible = TrackSet.Count > 1;
            
            tips.SetTip(TrackNumberSync, Catalog.GetString("Set all Track Numbers to this value"), "track numbers");
            tips.SetTip(TrackNumberIterator, Catalog.GetString("Automatically Set All Track Numbers"), "track iterator");
            tips.SetTip(TrackCountSync, Catalog.GetString("Set all Track Counts to this value"), "track counts");
            tips.SetTip(ArtistSync, Catalog.GetString("Set all Artists to this value"), "artists");
            tips.SetTip(AlbumSync, Catalog.GetString("Set all Albums to this value"), "albums");
            tips.SetTip(TitleSync, Catalog.GetString("Set all Titles to this value"), "titles");
            tips.SetTip(GenreSync, Catalog.GetString("Set all Genres to this value"), "genres");
                
            LoadTrack(0);
            
            try {
                AdvancedExpander.Expanded = (bool)Globals.Configuration.Get(GConfKeys.TrackPropertiesExpanded);
            } catch(Exception) {
                AdvancedExpander.Expanded = false;
            }
            
            AdvancedExpander.Activated += delegate(object o, EventArgs args) {
                Globals.Configuration.Set(GConfKeys.TrackPropertiesExpanded, AdvancedExpander.Expanded);
            };
                
            WindowTrackInfo.Show();
        }
        
        private string PrepareStatistic(string stat)
        {
            return "<small><i>" + stat + "</i></small>";
        }
        
        private void LoadTrack(int index)
        {
            if(index < 0 || index >= TrackSet.Count) {
                return;
            }
                
            EditorTrack track = TrackSet[index] as EditorTrack;
            
            AdvancedExpander.Visible = !(track.Track is AudioCdTrackInfo);
        
            TrackNumber.Value = track.TrackNumber;
            TrackCount.Value = track.TrackCount;
        
            (glade["Artist"] as Entry).Text = track.Artist;
            (glade["Album"] as Entry).Text = track.Album;
            (glade["Title"] as Entry).Text = track.Title;
            (glade["Genre"] as ComboBoxEntry).Entry.Text = track.Genre;
            
            (glade["DurationLabel"] as Label).Markup = PrepareStatistic(String.Format("{0}:{1}", 
                track.Track.Duration.Minutes, (track.Track.Duration.Seconds).ToString("00")));
            (glade["PlayCountLabel"] as Label).Markup = PrepareStatistic(track.Track.PlayCount.ToString());
            (glade["LastPlayedLabel"] as Label).Markup = PrepareStatistic(track.Track.LastPlayed == DateTime.MinValue ?
                Catalog.GetString("Never Played") : track.Track.LastPlayed.ToString());
            (glade["ImportedLabel"] as Label).Markup = PrepareStatistic(track.Track.DateAdded == DateTime.MinValue ?
                Catalog.GetString("Unknown") : track.Track.DateAdded.ToString());
                    
            string title = TrackSet.Count > 1 
                ? String.Format(Catalog.GetString("Editing Song {0} of {1}"), index + 1, TrackSet.Count)
                : Catalog.GetString("Editing Song");
       
            WindowTrackInfo.Title = title;
            TitleLabel.Markup = "<big><b>" + title + "</b></big>";
            
            Uri.Text = track.Uri.LocalPath;
            tips.SetTip(glade["UriTitle"], String.Format(Catalog.GetString("File: {0}"), Uri.Text), "uri");
            tips.SetTip(glade["Uri"], String.Format(Catalog.GetString("File: {0}"), Uri.Text), "uri");
            
            if(!(track.Track is AudioCdTrackInfo) && track.Uri.Scheme == System.Uri.UriSchemeFile) {
                try {
                    Entagged.AudioFile af = new Entagged.AudioFile(track.Uri.LocalPath, 
                        Banshee.Gstreamer.Utilities.DetectMimeType(track.Uri));
                    BitRate.Text = af.Bitrate.ToString() + " " + Catalog.GetString("KB/Second");
                    SampleRate.Text = String.Format(Catalog.GetString("{0} KHz"), (double)af.SampleRate / 1000.0);
                    Vbr.Text = af.IsVbr ? Catalog.GetString("Yes") : Catalog.GetString("No");
                    Channels.Text = af.Channels.ToString();
                    MimeType.Text = af.MimeType;
                    ExtraInfo.Text = af.EncodingType;
                } catch(Exception e) {
                    BitRate.Text = Catalog.GetString("Unknown");
                    SampleRate.Text = Catalog.GetString("Unknown");
                    Vbr.Text = Catalog.GetString("Unknown");
                    Channels.Text = Catalog.GetString("Unknown");
                    MimeType.Text = Catalog.GetString("Unknown");
                    ExtraInfo.Text = Catalog.GetString("Unknown");
                }
            }
            
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
            foreach(EditorTrack track in TrackSet) {
                track.TrackNumber = (uint)TrackNumber.Value;
            }
        }
        
        private void OnTrackNumberIteratorClicked(object o, EventArgs args)
        {
            int i = 1;
            foreach(EditorTrack track in TrackSet) {
                track.TrackNumber = (uint)i++;
                track.TrackCount = (uint)TrackSet.Count;
            }

            EditorTrack current_track = TrackSet[currentIndex] as EditorTrack;
            TrackNumber.Value = (int)current_track.TrackNumber;
            TrackCount.Value = (int)current_track.TrackCount;
        }
        
        private void OnValueEdited(object o, EventArgs args)
        {
            if(currentIndex < 0 || currentIndex >= TrackSet.Count) {
                return;
            }
            
            //SaveTrack(UpdateCurrent(), false);
        }
        
        private void OnTrackCountSyncClicked(object o, EventArgs args)
        {
            foreach(EditorTrack track in TrackSet) {
                track.TrackCount = (uint)TrackCount.Value;
            }
        }

        private void OnArtistSyncClicked(object o, EventArgs args)
        {
            foreach(EditorTrack track in TrackSet) {
                track.Artist = Artist.Text;
            }
        }

        private void OnAlbumSyncClicked(object o, EventArgs args)
        {
            foreach(EditorTrack track in TrackSet) {
                track.Album = Album.Text;
            }
        }
        
        private void OnTitleSyncClicked(object o, EventArgs args)
        {
            foreach(EditorTrack track in TrackSet) {
                track.Title = Title.Text;
            }
        }
        
        private void OnGenreSyncClicked(object o, EventArgs args)
        {
            foreach(EditorTrack track in TrackSet) {
                track.Genre = Genre.Entry.Text;
            }
        }
        
        private EditorTrack UpdateCurrent()
        {
            if(currentIndex < 0 || currentIndex >= TrackSet.Count) {
                return null;
            }
                
            EditorTrack track = TrackSet[currentIndex] as EditorTrack;
            
            track.TrackNumber = (uint)TrackNumber.Value;
            track.TrackCount = (uint)TrackCount.Value;
            track.Artist = Artist.Text;
            track.Album = Album.Text;
            track.Title = Title.Text;
            track.Genre = Genre.Entry.Text;
            
            return track;
        }

        private void OnCancelButtonClicked(object o, EventArgs args)
        {
            WindowTrackInfo.Destroy();
        }
        
        private void OnSaveButtonClicked(object o, EventArgs args)
        {
            UpdateCurrent();
            
            foreach(EditorTrack track in TrackSet) {
                SaveTrack(track, true);
            }

            EventHandler handler = Saved;
            if(handler != null) {
                handler(this, new EventArgs());
            }
            
            WindowTrackInfo.Destroy();
        }
        
        private void SaveTrack(EditorTrack track, bool writeToDatabase)
        {
            track.Save();
            
            if(writeToDatabase) {
                track.Track.Save();
            }
                
            if(track.Track == PlayerEngineCore.CurrentTrack) {
                PlayerUI.Instance.UpdateMetaDisplay();
            }
        }
    }
}
