//
// AudioCdDisc.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Unix;
using MusicBrainz;

using Hyena;
using Banshee.Base;
using Banshee.Hardware;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.AudioCd
{
    public class AudioCdDiscModel : MemoryTrackListModel
    {
        private IDiscVolume volume;
        
        public event EventHandler MetadataQueryStarted;
        public event EventHandler MetadataQueryFinished;
        public event EventHandler EnabledCountChanged;
        
        private bool metadata_query_success;
        private DateTime metadata_query_start_time;
        
        public bool MetadataQuerySuccess {
            get { return metadata_query_success; }
        }
        
        private TimeSpan duration;
        public TimeSpan Duration {
            get { return duration; }
        }
        
        public AudioCdDiscModel (IDiscVolume volume)
        {
            this.volume = volume;
            disc_title = Catalog.GetString ("Audio CD");
        }
        
        public void LoadModelFromDisc ()
        {
            Clear ();
        
            LocalDisc mb_disc = LocalDisc.GetFromDevice (volume.DeviceNode);
            if (mb_disc == null) {
                throw new ApplicationException ("Could not read contents of the disc. Platform may not be supported.");
            }
            
            for (int i = 0, n = mb_disc.TrackDurations.Length; i < n; i++) {
                AudioCdTrackInfo track = new AudioCdTrackInfo (this, volume.DeviceNode, i);
                track.TrackNumber = i + 1;
                track.TrackCount = n;
                track.Disc = 1;
                track.Duration = TimeSpan.FromSeconds (mb_disc.TrackDurations[i]);
                track.ArtistName = Catalog.GetString ("Unknown Artist");
                track.AlbumTitle = Catalog.GetString ("Unknown Album");
                track.TrackTitle = String.Format(Catalog.GetString ("Track {0}"), track.TrackNumber);
                Add (track);
                
                duration += track.Duration;
            }
            
            EnabledCount = Count;
            
            Reload ();
            
            ThreadPool.QueueUserWorkItem (LoadDiscMetadata, mb_disc);
        }
        
        private void LoadDiscMetadata (object state)
        {
            LocalDisc mb_disc = (LocalDisc)state;
            
            OnMetadataQueryStarted (mb_disc);
            
            Release release = Release.Query (mb_disc).PerfectMatch ();
            if (release == null || release.Tracks.Count != Count) {
                OnMetadataQueryFinished (false);
                return;
            }
                        
            disc_title = release.Title;
            
            int disc_number = 1;
            int i = 0;
            
            foreach (Disc disc in release.Discs) {
                i++;
                if (disc.Id == mb_disc.Id) {
                    disc_number = i;
                }
            }
            
            DateTime release_date = DateTime.MinValue;
            int release_event_index = -1;
            
            for (i = 0; i < release.Events.Count; i++) {
                // FIXME: This is sort of lame, but from what I've seen, 
                // the US releases generally contain more info
                if (release.Events[i].Country == "US") {
                    release_event_index = i;
                    break;
                }
            }
            
            if (release_event_index >= 0) {
                release_date = DateTime.Parse (release.Events[release_event_index].Date, 
                    ApplicationContext.InternalCultureInfo);
            }
            
            DatabaseArtistInfo artist = new DatabaseArtistInfo ();
            artist.Name = release.Artist.Name;
            artist.MusicBrainzId = release.Artist.Id;
            
            DatabaseAlbumInfo album = new DatabaseAlbumInfo ();
            album.Title = release.Title;
            album.ArtistName = artist.Name;
            album.MusicBrainzId = release.Id;
            album.ReleaseDate = release_date;
            
            i = 0;
            
            foreach (Track track in release.Tracks) {
                AudioCdTrackInfo model_track = (AudioCdTrackInfo)this[i++];
                
                model_track.MusicBrainzId = track.Id;
                model_track.TrackTitle = track.Title;
                model_track.ArtistName = track.Artist.Name;
                model_track.AlbumTitle = release.Title;
                model_track.Disc = disc_number;
                
                model_track.Album = album;
                model_track.Artist = new DatabaseArtistInfo ();
                model_track.Artist.Name = track.Artist.Name;
                model_track.Artist.MusicBrainzId = track.Artist.Id;
                
                if (!release_date.Equals (DateTime.MinValue)) {
                    model_track.Year = release_date.Year;
                }
            }
            
            OnMetadataQueryFinished (true);
        }
        
        private void OnMetadataQueryStarted (LocalDisc mb_disc)
        {
            metadata_query_success = false;
            metadata_query_start_time = DateTime.Now;
            Log.InformationFormat ("Querying MusicBrainz for Disc Release ({0})", mb_disc.Id);
        
            ThreadAssist.ProxyToMain (delegate { 
                EventHandler handler = MetadataQueryStarted;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            });
        }
        
        private void OnMetadataQueryFinished (bool success)
        {
            metadata_query_success = success;
            Log.InformationFormat ("Query finished (success: {0}, {1} seconds)", 
                success, (DateTime.Now - metadata_query_start_time).TotalSeconds);
            
            ThreadAssist.ProxyToMain (delegate {
                Reload ();
                
                EventHandler handler = MetadataQueryFinished;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            });
        }
        
        private void OnEnabledCountChanged ()
        {
            EventHandler handler = EnabledCountChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        private ICdromDevice Drive {
            get { return Volume == null ? null : (Volume.Parent as ICdromDevice); }
        }
        
        public bool LockDoor ()
        {
            ICdromDevice drive = Drive;
            return drive != null ? drive.LockDoor () : false;
        }
        
        public bool UnlockDoor ()
        {
            ICdromDevice drive = Drive;
            return drive != null ? drive.UnlockDoor () : false;
        }
        
        public bool IsDoorLocked {
            get { 
                ICdromDevice drive = Drive;
                return drive != null ? drive.IsDoorLocked : false;
            }
        }
        
        public IDiscVolume Volume {
            get { return volume; }
        }
        
        private string disc_title;
        public string Title {
            get { return disc_title; }
        }
        
        private int enabled_count;
        public int EnabledCount {
            get { return enabled_count; }
            internal set { 
                enabled_count = value; 
                OnEnabledCountChanged (); 
            }
        }
    }
}
