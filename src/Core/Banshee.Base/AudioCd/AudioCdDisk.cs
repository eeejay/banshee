/***************************************************************************
 *  AudioCdDisk.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using MusicBrainz;

using Banshee.Collection;

namespace Banshee.Base
{
    public enum AudioCdLookupStatus {
        ReadingDisk,
        SearchingMetadata,
        SearchingCoverArt,
        Success,
        ErrorNoConnection,
        ErrorLookup
    }
    
    public class AudioCdDisk
    {
        private string udi;
        private string device_node;
        private string drive_name;
        
        private bool mb_querying = false;
        private bool mb_queried = false;
        private AudioCdLookupStatus status = AudioCdLookupStatus.ReadingDisk;
        
        private string album_title;
        
        private List<TrackInfo> tracks = new List<TrackInfo>();
        
        public event EventHandler Updated;

        public AudioCdDisk(string udi, string deviceNode, string driveName)
        {
            this.udi = udi;
            device_node = deviceNode;
            drive_name = driveName;
            
            Globals.Network.StateChanged += OnNetworkStateChanged;
            
            Status = AudioCdLookupStatus.ReadingDisk;
            LoadDiskInfo();
        }
              
        private void LoadDiskInfo()
        {
            tracks.Clear();
            SimpleDisc mb_disc = new SimpleDisc(device_node);
            //mb_disc.Client.Debug = true;
            
            foreach(SimpleTrack mb_track in mb_disc) {
                AudioCdTrackInfo track = new AudioCdTrackInfo(this);
                track.Duration = new TimeSpan(mb_track.Length * TimeSpan.TicksPerSecond);
                track.TrackIndex = mb_track.Index;
                track.Artist = Catalog.GetString("Unknown Artist");
                track.Album = Catalog.GetString("Unknown Album");
                track.Title = String.Format(Catalog.GetString("Track {0}"), mb_track.Index);
                
                tracks.Add(track);
            }
            
            album_title = Catalog.GetString("Audio CD");
            
            QueryMetadata(mb_disc);
        }
        
        private void OnNetworkStateChanged(object o, NetworkStateChangedArgs args)
        {
            if(!mb_queried && args.Connected) {
                QueryMetadata();
            }
        }
        
        public void QueryMetadata()
        {
            QueryMetadata(null);
        }
        
        private void QueryMetadata(SimpleDisc disc)
        {
            ThreadPool.QueueUserWorkItem(QueryMusicBrainz, disc);
        }
        
        private void QueryMusicBrainz(object o)
        {
            if(mb_querying) {
                return;
            }
            
            mb_querying = true;
            
            if(!Globals.Network.Connected) {
                Status = AudioCdLookupStatus.ErrorNoConnection;
                mb_querying = false;
                return;
            }
            
            Status = AudioCdLookupStatus.SearchingMetadata;
            
            SimpleDisc mb_disc;
            
            if(o == null) {
                mb_disc = new SimpleDisc(device_node);
            } else {
                mb_disc = o as SimpleDisc;
            }
            
            try {
                mb_disc.QueryCDMetadata();
            } catch {
                Status = AudioCdLookupStatus.ErrorLookup;
                mb_querying = false;
                return;
            }
            
            int min = tracks.Count < mb_disc.Tracks.Length 
                ? tracks.Count : mb_disc.Tracks.Length;
            
            if(mb_disc.AlbumName != null) {
                album_title = mb_disc.AlbumName;
            } 
            
            for(int i = 0; i < min; i++) {
                tracks[i].Duration = new TimeSpan(mb_disc[i].Length * TimeSpan.TicksPerSecond);
                (tracks[i] as AudioCdTrackInfo).TrackIndex = mb_disc[i].Index;
                
                if(mb_disc[i].Artist != null) {
                    tracks[i].Artist = mb_disc[i].Artist;
                }
                
                if(mb_disc.AlbumName != null) {
                    tracks[i].Album = mb_disc.AlbumName;
                }
                
                if(mb_disc[i].Title != null) {
                    tracks[i].Title = mb_disc[i].Title;
                }
                
                tracks[i].Asin = mb_disc.AmazonAsin;
                tracks[i].RemoteLookupStatus = RemoteLookupStatus.Success;
            }
            
            string asin = mb_disc.AmazonAsin;
            
            if(asin == null || asin == String.Empty) {
                HandleUpdated();
            
                // sometimes ASINs aren't associated with a CD disc ID, but they are associated
                // with file track metadata. If no ASIN was returned for the CD lookup, use the
                // first track on the CD to attempt a file lookup
                try {
                    SimpleTrack mb_track = SimpleQuery.FileLookup(mb_disc.Client,
                        tracks[0].Artist, tracks[0].Album, tracks[0].Title, 0, 0);
                    asin = mb_track.Asin;
                    for(int i = 0; i < min; i++) {
                        tracks[i].Asin = asin;
                    }
                } catch {
                }
            }
            
            mb_queried = true;
            mb_disc.Dispose();
            HandleUpdated();
            
            string path = Paths.GetCoverArtPath(asin);
            if(System.IO.File.Exists(path)) {
                Status = AudioCdLookupStatus.Success;
                mb_querying = false;
                return;
            }
            
            Status = AudioCdLookupStatus.SearchingCoverArt;
            
            Banshee.Metadata.MusicBrainz.MusicBrainzQueryJob cover_art_job =
                new Banshee.Metadata.MusicBrainz.MusicBrainzQueryJob(tracks[0],
                    Banshee.Metadata.MetadataService.Instance.Settings, asin);
            if(cover_art_job.Lookup()) {
                HandleUpdated();
            }
            
            Status = AudioCdLookupStatus.Success;
            mb_querying = false;
        }
        
        private void HandleUpdated()
        {
            ThreadAssist.ProxyToMain(delegate {
                EventHandler handler = Updated;
                if(handler != null) {
                    handler(this, new EventArgs());
                }
            });
        }
        
        [DllImport("libc")]
        private static extern int ioctl(int device, EjectOperation request); 

        private enum EjectOperation {
            Open = 0x5309,
            Close = 0x5319
        }

        public bool Eject()
        {
            return Eject(true);
        }

        public bool Eject(bool open)
        {
            try {
                if(IsRipping) {
                    LogCore.Instance.PushWarning(Catalog.GetString("Cannot Eject CD"),
                        Catalog.GetString("The CD cannot be ejected while it is importing. Stop the import first."));
                    return false;
                }
                
                AudioCdTrackInfo track = PlayerEngineCore.CurrentTrack as AudioCdTrackInfo;
                if(track != null && track.Device == DeviceNode) {
                    PlayerEngineCore.Close();
                }
            
                Hal.Device device = new Hal.Device(udi);
                if(device.IsVolume && device.GetPropertyBoolean("volume.is_mounted")) {
                    try {
                        device.Volume.Unmount();
                        return true;
                    } catch {
                        return false;
                    }
                }
            
                using(UnixStream stream = (new UnixFileInfo(device_node)).Open( 
                    Mono.Unix.Native.OpenFlags.O_RDONLY | 
                    Mono.Unix.Native.OpenFlags.O_NONBLOCK)) {
                    return ioctl(stream.Handle, open
                        ? EjectOperation.Open
                        : EjectOperation.Close) == 0;
                }
            } catch {
                return false;
            }
        }
        
        public AudioCdLookupStatus Status {
            get { return status; }
            
            private set {
                status = value;
                HandleUpdated();
            }
        }
        
        public string Udi { 
            get { return udi; }
        }
        
        public string DeviceNode { 
            get { return device_node; }
        }
        
        public string DriveName { 
            get { return drive_name; }
        }
        
        public string Title { 
            get { return album_title; }
        }
        
        public int TrackCount { 
            get { return tracks.Count; }
        }
        
        public bool Valid { 
            get { return true; }
        }
        
        public IEnumerable<TrackInfo> Tracks { 
            get { return tracks; }
        }
        
        private bool is_ripping = false;
        public bool IsRipping {
            get { return is_ripping; }
            set { is_ripping = value; }
        }
    }
}
