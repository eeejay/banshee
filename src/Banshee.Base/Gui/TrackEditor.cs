/***************************************************************************
 *  TrackEditor.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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

namespace Banshee.Gui.Dialogs
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
        public int Year;
    
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
            Year = track.Year;
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
            track.Year = Year;
        }
        
        public TrackInfo Track {
            get { return track; }
        }
    }

    public class TrackEditor : GladeWindow
    {
        [Widget] private Window WindowTrackInfo;
        [Widget] private Button CancelButton;
        [Widget] private Button SaveButton;
        [Widget] private Button Previous;
        [Widget] private Button Next;
        [Widget] private Button TrackNumberIterator;
        [Widget] private Button TrackCountSync;
        [Widget] private Button ArtistSync;
        [Widget] private Button AlbumSync;
        [Widget] private Button YearSync;
        [Widget] private Button GenreSync;
        [Widget] private SpinButton TrackCount;
        [Widget] private SpinButton TrackNumber;
        [Widget] private Entry Year;
        [Widget] private Entry Artist;
        [Widget] private Entry Album;
        [Widget] private Entry Title;
        [Widget] private ComboBoxEntry Genre;
        [Widget] private Entry Uri;
        [Widget] private Entry Location;
        [Widget] private Label BitRate;
        [Widget] private Label SampleRate;
        [Widget] private Label Channels;
        [Widget] private Label FileSize;
        [Widget] private Button SyncAll;
        
        Tooltips tips = new Tooltips();
        
        private ArrayList TrackSet = new ArrayList();
        private int currentIndex = 0;

        public event EventHandler Saved;

        public TrackEditor(TrackInfo [] selection) : base("WindowTrackInfo")
        {
            if(selection == null) {
                return;
            }
        
            foreach(TrackInfo track in selection) {
                TrackSet.Add(new EditorTrack(track));
            }

            TrackNumberIterator.ExposeEvent += OnTrackNumberIteratorExpose;

            CancelButton.Clicked += OnCancelButtonClicked;
            SaveButton.Clicked += OnSaveButtonClicked;
            Previous.Clicked += OnPreviousClicked;
            Next.Clicked += OnNextClicked;
            
            TrackNumberIterator.Clicked += OnTrackNumberIteratorClicked;
            TrackCountSync.Clicked += OnTrackCountSyncClicked;
            ArtistSync.Clicked += OnArtistSyncClicked;
            AlbumSync.Clicked += OnAlbumSyncClicked;
            GenreSync.Clicked += OnGenreSyncClicked;
            YearSync.Clicked += OnYearSyncClicked;
            SyncAll.Clicked += OnSyncAllClicked;
            
            Artist.Changed += OnValueEdited;
            Album.Changed += OnValueEdited;
            Title.Changed += OnValueEdited;
            Year.Changed += OnValueEdited;
            Genre.Entry.Changed += OnValueEdited;
            ListStore genre_model = new ListStore(typeof(string));

            Genre.Model = genre_model;
            Genre.TextColumn = 0;
            
            foreach(string genre in Globals.Library.GetGenreList()) {
                genre_model.AppendValues(genre);            
            }
            
            Next.Visible = TrackSet.Count > 1;
            Previous.Visible = TrackSet.Count > 1;

            Glade["MultiTrackButtons"].Visible = TrackSet.Count > 1;
            TrackNumberIterator.Visible = TrackSet.Count > 1;
            TrackCountSync.Visible = TrackSet.Count > 1;
            ArtistSync.Visible = TrackSet.Count > 1;
            AlbumSync.Visible = TrackSet.Count > 1;
            GenreSync.Visible = TrackSet.Count > 1;
            YearSync.Visible = TrackSet.Count > 1;
            Glade["SyncAllAlignment"].Visible = TrackSet.Count > 1;

            tips.SetTip(TrackNumberIterator, Catalog.GetString("Automatically set all track numbers in increasing order"), "track iterator");
            tips.SetTip(TrackCountSync, Catalog.GetString("Set all track counts to this value"), "track counts");
            tips.SetTip(ArtistSync, Catalog.GetString("Set all artists to this value"), "artists");
            tips.SetTip(AlbumSync, Catalog.GetString("Set all albums to this value"), "albums");
            tips.SetTip(GenreSync, Catalog.GetString("Set all genres to this value"), "genres");
            tips.SetTip(YearSync, Catalog.GetString("Set all years to this value"), "years");
            tips.SetTip(SyncAll, Catalog.GetString("Set all common fields in all selected tracks to the values currently set"), "all");

            LoadTrack(0);
                
            WindowTrackInfo.Show();
        }
        
        private void OnTrackNumberIteratorExpose(object o, ExposeEventArgs args)
        {
            Gdk.Rectangle alloc = TrackNumberIterator.Allocation;
            Gdk.GC gc = TrackNumberIterator.Style.DarkGC(StateType.Normal);
            Gdk.Drawable drawable = TrackNumberIterator.GdkWindow;
            
            int x_pad = (int)((double)alloc.Width * 0.15);
            int y_pad = (int)((double)alloc.Height * 0.15);
            
            int left_x = alloc.X + x_pad;
            int top_y = alloc.Y + y_pad;
            int mid_x = alloc.X + ((alloc.Width / 2) - x_pad) + 2; 
            int mid_y = alloc.Y + (alloc.Height / 2);
            int bottom_y = (alloc.Y + alloc.Height) - y_pad;
            
            drawable.DrawLine(gc, left_x, top_y, mid_x, top_y);
            drawable.DrawLine(gc, mid_x + 1, top_y + 1, mid_x + 1, mid_y - 15);
            
            drawable.DrawLine(gc, left_x, bottom_y, mid_x, bottom_y);
            drawable.DrawLine(gc, mid_x + 1, bottom_y - 1, mid_x + 1, mid_y + 13);
        }
        
        private void LoadTrack(int index)
        {
            if(index < 0 || index >= TrackSet.Count) {
                return;
            }
                
            EditorTrack track = TrackSet[index] as EditorTrack;
            
            TrackNumber.Value = track.TrackNumber;
            TrackCount.Value = track.TrackCount;
            Year.Text = track.Year.ToString();
        
            (Glade["Artist"] as Entry).Text = track.Artist;
            (Glade["Album"] as Entry).Text = track.Album;
            (Glade["Title"] as Entry).Text = track.Title;
            (Glade["Genre"] as ComboBoxEntry).Entry.Text = track.Genre;
            
            (Glade["DurationLabel"] as Label).Text = String.Format("{0}:{1}", 
                track.Track.Duration.Minutes, (track.Track.Duration.Seconds).ToString("00"));
            (Glade["PlayCountLabel"] as Label).Text = track.Track.PlayCount.ToString();
            (Glade["LastPlayedLabel"] as Label).Text = track.Track.LastPlayed == DateTime.MinValue ?
                Catalog.GetString("Never played") : track.Track.LastPlayed.ToString();
            (Glade["ImportedLabel"] as Label).Text = track.Track.DateAdded == DateTime.MinValue ?
                Catalog.GetString("Unknown") : track.Track.DateAdded.ToString();
                    
            WindowTrackInfo.Title = TrackSet.Count > 1 
                ? String.Format(Catalog.GetString("Editing song {0} of {1}"), index + 1, TrackSet.Count)
                : String.Format(Catalog.GetString("Editing {0}"), track.Title);
       
            if(track.Uri.IsLocalPath) {
                Uri.Text = System.IO.Path.GetFileName(track.Uri.LocalPath);
                Location.Text = System.IO.Path.GetDirectoryName(track.Uri.LocalPath);
            } else {
                Uri.Text = track.Uri.ToString();
                Location.Text = String.Empty;
            }
            
            if(!(track.Track is AudioCdTrackInfo)) {
                FileSize.Text = Catalog.GetString("Unknown");
                
                if(track.Uri.Scheme == System.Uri.UriSchemeFile) {
                    try {
                        System.IO.FileInfo info = new System.IO.FileInfo(track.Uri.LocalPath);
                        FileSize.Text = String.Format("{0:0.0} MB", (double)info.Length / 1024.0 / 1024.0);
                    } catch {
                    }
                }
                
                try {
                    TagLib.File file = StreamTagger.ProcessUri(track.Uri);
                    if(file.AudioProperties != null) {
                        BitRate.Text = String.Format("{0} kbps", file.AudioProperties.Bitrate);
                        SampleRate.Text = String.Format("{0} Hz", file.AudioProperties.SampleRate);
                        Channels.Text = String.Format("{0}", file.AudioProperties.Channels);
                    } else {
                        throw new Exception();
                    }
                } catch(Exception e) {
                    BitRate.Text = Catalog.GetString("Unknown");
                    SampleRate.Text = Catalog.GetString("Unknown");
                    Channels.Text = Catalog.GetString("Unknown");
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
        }
        
        private void OnTrackCountSyncClicked(object o, EventArgs args)
        {
            foreach(EditorTrack track in TrackSet) {
                track.TrackCount = (uint)TrackCount.Value;
            }
        }

        private void OnYearSyncClicked(object o, EventArgs args)
        {
            foreach(EditorTrack track in TrackSet) {
                try {
                    track.Year = Convert.ToInt32(Year.Text);
                } catch {
                    track.Year = 0;
                }
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
        
        private void OnSyncAllClicked(object o, EventArgs args)
        {
            OnTrackCountSyncClicked(o, args);
            OnGenreSyncClicked(o, args);
            OnAlbumSyncClicked(o, args);
            OnArtistSyncClicked(o, args);
            OnYearSyncClicked(o, args);
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
            try {
                track.Year = Convert.ToInt32(Year.Text);
            } catch {
                track.Year = 0;
            }
            
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
                
                try {
                    if((bool)Globals.Configuration.Get(GConfKeys.WriteMetadata)) {
                        SaveToFile(track);
                    }
                } catch(GConf.NoSuchKeyException) {
                }
            }
                
            if(track.Track == PlayerEngineCore.CurrentTrack) {
                PlayerEngineCore.TrackInfoUpdated();
            }
        }
        
        private void SaveToFile(EditorTrack track)
        {
            Banshee.Kernel.Scheduler.Schedule(new SaveTrackMetadataJob(track.Track), 
                Banshee.Kernel.JobPriority.Highest);
        }
    }
}
